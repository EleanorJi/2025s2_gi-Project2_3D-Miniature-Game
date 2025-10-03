// using UnityEngine;

// public class CheckpointWithAnchor : MonoBehaviour
// {
//     [Tooltip("重生落点（不填则用本物体位置）")]
//     public Transform respawnAnchor;

//     public bool oneShot = true;

//     void OnTriggerEnter(Collider other)
//     {
//         if (!other.CompareTag("Player")) return;
//         var pc = other.GetComponent<PlayerController>();
//         if (!pc) return;

//         pc.respawnPoint = respawnAnchor ? respawnAnchor : this.transform; // ✅ 用锚点
//         Debug.Log($"[Checkpoint] {name} -> respawn at {(respawnAnchor ? respawnAnchor.position : transform.position)}");

//         if (oneShot) GetComponent<Collider>().enabled = false;
//     }
// }
