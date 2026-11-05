using Nykwil.GekkoNet.Session;
namespace SharedGame.Samples.UnityTransport {
    public sealed class UnityTransportDuel : RollbackDuel {
        protected override void OnEnable() {
            // Multiple standalone peers must keep polling when another window has focus.
            UnityEngine.Application.runInBackground = true;
            base.OnEnable();
        }
        protected override IGekkoNetworkTransport CreateTransport(SessionOptions options) => UnityTransportAdapter.Create(options);
    }
}
