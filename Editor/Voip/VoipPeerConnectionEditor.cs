using UnityEditor;

namespace Ubiq.Voip
{
    [CustomEditor(typeof(VoipPeerConnection))]
    public class VoipPeerConnectionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var pc = target as VoipPeerConnection;

            if (!string.IsNullOrEmpty(pc.peerUuid))
            {
                EditorGUILayout.LabelField("Peer", pc.peerUuid);
                EditorGUILayout.LabelField("Peer State", pc.peerConnectionState.ToString());
                EditorGUILayout.LabelField("Ice State", pc.iceConnectionState.ToString());
            }
        }
    }
}