using UnityEditor;

namespace Ubiq.Voip
{
    [CustomEditor(typeof(VoipPeerConnection))]
    public class VoipPeerConnectionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var pc = target as VoipPeerConnection;

            if (pc.isSetup)
            {
                EditorGUILayout.LabelField("Peer", pc.PeerUuid);
                EditorGUILayout.LabelField("Peer State", pc.peerConnectionState.ToString());
                EditorGUILayout.LabelField("Ice State", pc.iceConnectionState.ToString());
            }
        }
    }
}