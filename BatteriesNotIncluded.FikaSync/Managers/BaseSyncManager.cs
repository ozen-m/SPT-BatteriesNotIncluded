using System;
using BatteriesNotIncluded.Managers;
using UnityEngine;

namespace BatteriesNotIncluded.FikaSync.Managers;

public abstract class BaseSyncManager : MonoBehaviour
{
    [NonSerialized]
    public DeviceManager DeviceManager;
}
