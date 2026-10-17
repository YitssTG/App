using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>Android speech recognition; callbacks are marshalled back to Unity.</summary>
[Preserve] public sealed class AMIVoice : MonoBehaviour
{
    public bool Busy { get; private set; }
    public event Action<string> Status;
    private readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
    private Action<string> result;
    private volatile int session;
    private bool awaitingPermission;
    private float deadline;
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject recognizer;
    private Listener listener;
    private PermissionCallbacks permissions;
#endif

    private void Update()
    {
        while (pending.TryDequeue(out var action)) action();
        if (Busy && Time.realtimeSinceStartup > deadline)
        {
            Cancel();
            Status?.Invoke("Se agotó el tiempo de escucha. Puedes volver a dictar o escribir.");
        }
    }

    public void Begin(bool allowSystemService, Action<string> onResult)
    {
        if (Busy) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        Busy = true;
        result = onResult;
        deadline = Time.realtimeSinceStartup + 90;
        int token = ++session;
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            awaitingPermission = true;
            Status?.Invoke("Autoriza el micrófono para dictar. También puedes escribir.");
            permissions = new PermissionCallbacks();
            permissions.PermissionGranted += _ => pending.Enqueue(() => { if (token == session) { awaitingPermission = false; StartNative(token, allowSystemService); } });
            permissions.PermissionDenied += _ => pending.Enqueue(() => Fail(token, "Micrófono no autorizado. Puedes habilitarlo en Ajustes de Android o escribir."));
            Permission.RequestUserPermission(Permission.Microphone, permissions);
            return;
        }
        StartNative(token, allowSystemService);
#else
        Status?.Invoke("El dictado real está disponible en Android. En Unity puedes probar el formulario escribiendo.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject Activity()
    {
        using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            return player.GetStatic<AndroidJavaObject>("currentActivity");
    }
    private void StartNative(int token, bool allowSystemService)
    {
        Status?.Invoke("Preparando el micrófono…");
        using (var activity = Activity()) activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            if (token != session) return;
            try
            {
                using (var context = Activity())
                using (var api = new AndroidJavaClass("android.speech.SpeechRecognizer"))
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    bool local = version.GetStatic<int>("SDK_INT") >= 31 && api.CallStatic<bool>("isOnDeviceRecognitionAvailable", context);
                    if (!local && !allowSystemService)
                    {
                        pending.Enqueue(() => Fail(token, "No hay reconocimiento local disponible. Puedes habilitar el servicio de voz de Android abajo o escribir."));
                        return;
                    }
                    if (!local && !api.CallStatic<bool>("isRecognitionAvailable", context))
                    {
                        pending.Enqueue(() => Fail(token, "No hay un servicio de reconocimiento instalado. Configura la entrada de voz de Android o escribe."));
                        return;
                    }
                    recognizer = api.CallStatic<AndroidJavaObject>(local ? "createOnDeviceSpeechRecognizer" : "createSpeechRecognizer", context);
                    listener = new Listener(this, token);
                    recognizer.Call("setRecognitionListener", listener);
                    using (var intent = new AndroidJavaObject("android.content.Intent", "android.speech.action.RECOGNIZE_SPEECH"))
                    {
                        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form").Dispose();
                        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE", "es-PE").Dispose();
                        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.PARTIAL_RESULTS", false).Dispose();
                        intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.MAX_RESULTS", 1).Dispose();
                        recognizer.Call("startListening", intent);
                    }
                }
            }
            catch { pending.Enqueue(() => Fail(token, "No se pudo iniciar el dictado. Revisa el servicio de voz y vuelve a intentarlo.")); }
        }));
    }

    [Preserve] private sealed class Listener : AndroidJavaProxy
    {
        private readonly AMIVoice owner;
        private readonly int token;
        public Listener(AMIVoice value, int id) : base("android.speech.RecognitionListener") { owner = value; token = id; }
        [Preserve] public void onReadyForSpeech(AndroidJavaObject bundle) { owner.pending.Enqueue(() => { if (token == owner.session) owner.Status?.Invoke("Escuchando… Habla con claridad. Pulsa Terminar cuando acabes."); }); bundle?.Dispose(); }
        [Preserve] public void onBeginningOfSpeech() { }
        [Preserve] public void onRmsChanged(float rms) { }
        [Preserve] public void onBufferReceived(byte[] buffer) { }
        [Preserve] public void onEndOfSpeech() { owner.pending.Enqueue(() => { if (token == owner.session) owner.Status?.Invoke("Convirtiendo la voz en texto…"); }); }
        [Preserve] public void onPartialResults(AndroidJavaObject bundle) { bundle?.Dispose(); }
        [Preserve] public void onEvent(int type, AndroidJavaObject bundle) { bundle?.Dispose(); }
        [Preserve] public void onResults(AndroidJavaObject bundle)
        {
            string text = "";
            try
            {
                using (var values = bundle.Call<AndroidJavaObject>("getStringArrayList", "results_recognition"))
                    if (values != null && values.Call<int>("size") > 0) text = values.Call<string>("get", 0);
            }
            finally { bundle?.Dispose(); }
            var captured = text;
            owner.pending.Enqueue(() => owner.Complete(token, captured));
        }
        [Preserve] public void onError(int error)
        {
            string message;
            switch (error)
            {
                case 1: case 2: case 4: message = "El servicio de voz no pudo conectarse. Revisa Internet o usa dictado local."; break;
                case 6: case 7: message = "No se reconoció una frase. Acerca el teléfono y vuelve a dictar."; break;
                case 9: message = "El micrófono no tiene permiso. Puedes autorizarlo en Ajustes de Android."; break;
                case 12: case 13: message = "El español no está disponible en este servicio. Descarga el idioma en los ajustes de voz de Android o usa otro servicio."; break;
                case 8: message = "El micrófono está ocupado. Espera unos segundos y vuelve a intentar."; break;
                default: message = "El dictado se interrumpió (" + error + "). Tu texto anterior se conserva."; break;
            }
            owner.pending.Enqueue(() => owner.Fail(token, message));
        }
    }
#endif

    private void Complete(int token, string text)
    {
        if (token != session) return;
        var callback = result;
        Cancel();
        if (string.IsNullOrWhiteSpace(text)) { Status?.Invoke("No se reconoció texto. Intenta nuevamente."); return; }
        callback?.Invoke(text.Trim());
        Status?.Invoke("Texto recibido. Revísalo y corrige lo necesario antes de guardar.");
    }
    private void Fail(int token, string message)
    {
        if (token != session) return;
        Cancel();
        Status?.Invoke(message);
    }
    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int token = session;
        using (var activity = Activity()) activity.Call("runOnUiThread", new AndroidJavaRunnable(() => { if (token != session) return; try { recognizer?.Call("stopListening"); } catch { } }));
#endif
    }
    public void Cancel()
    {
        ++session;
        Busy = false;
        awaitingPermission = false;
        result = null;
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var activity = Activity()) activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            var old = recognizer; recognizer = null;
            if (old == null) return;
            try { old.Call("cancel"); old.Call("destroy"); } finally { old.Dispose(); }
        }));
#endif
    }
    private void OnApplicationPause(bool pause) { if (pause && Busy && !awaitingPermission) { Cancel(); Status?.Invoke("Dictado detenido al salir de la app. Puedes iniciarlo otra vez."); } }
    private void OnDestroy() { Cancel(); }
}
