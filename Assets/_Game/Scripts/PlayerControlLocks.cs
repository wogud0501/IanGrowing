using System;using System.Collections.Generic;using UnityEngine;
namespace IanGame {
// Each owner releases only its lease. Scene disable invalidates outstanding leases.
public sealed class PlayerControlLocks:MonoBehaviour {
 readonly Dictionary<string,int> owners=new Dictionary<string,int>();int generation;
 public bool Locked=>owners.Count>0;public int OwnerCount=>owners.Count;
 public IDisposable Acquire(string owner){if(string.IsNullOrEmpty(owner))throw new ArgumentException(nameof(owner));owners.TryGetValue(owner,out int n);owners[owner]=n+1;return new Lease(this,owner,generation);}
 void Release(string owner,int epoch){if(epoch!=generation||!owners.TryGetValue(owner,out int n))return;if(n<=1)owners.Remove(owner);else owners[owner]=n-1;}
 sealed class Lease:IDisposable {PlayerControlLocks target;readonly string owner;readonly int epoch;public Lease(PlayerControlLocks t,string o,int e){target=t;owner=o;epoch=e;}public void Dispose(){target?.Release(owner,epoch);target=null;}}
 void OnDisable(){generation++;owners.Clear();}
}
}
