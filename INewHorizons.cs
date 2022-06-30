using System.Collections.Generic;
using OWML.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TheVision.Utilities.ModAPIs 
{ 

    public interface INewHorizons
    {
        void Create(Dictionary<string, object> config, IModBehaviour mod);

        void LoadConfigs(IModBehaviour mod);

        GameObject GetPlanet(string name);

        string GetCurrentStarSystem(); 

        UnityEvent<string> GetChangeStarSystemEvent();

        UnityEvent<string> GetStarSystemLoadedEvent();

        GameObject SpawnObject(GameObject planet, Sector sector, string propToCopyPath, Vector3 position, Vector3 eulerAngles, float scale, bool alignWithNormal);

        
    }
}