
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class NewsStorage : UdonSharpBehaviour
{
    public NewsItem[] newsItems;
    public long refreshedAt = 0;
}
