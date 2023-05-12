using UnityEditor;

namespace Ubiq.Voip
{
    [CustomEditor(typeof(VoipPeerConnection))]
    public class VoipPeerConnectionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var pc = target as VoipPeerConnection;

            if (!string.IsNullOrEmpty(pc.PeerUuid))
            {
                EditorGUILayout.LabelField("Peer", pc.PeerUuid);
                EditorGUILayout.LabelField("Connection State", pc.peerConnectionState.ToString());
                EditorGUILayout.LabelField("Ice State", pc.iceConnectionState.ToString());
                if (pc.Polite)
                {
                    EditorGUILayout.LabelField("Polite");
                }
                else
                {
                    EditorGUILayout.LabelField("Impolite");

                }
            }
        }
    }
}