using System;
using System.Collections.Generic;
using OWML.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    public interface IMenuAPI
    {
        GameObject TitleScreen_MakeMenuOpenButton(string name, int index, Menu menuToOpen);
        GameObject TitleScreen_MakeSceneLoadButton(string name, int index, SubmitActionLoadScene.LoadableScenes sceneToLoad, PopupMenu confirmPopup = null);
        Button TitleScreen_MakeSimpleButton(string name, int index);
        GameObject PauseMenu_MakeMenuOpenButton(string name, Menu menuToOpen, Menu customMenu = null);
        GameObject PauseMenu_MakeSceneLoadButton(string name, SubmitActionLoadScene.LoadableScenes sceneToLoad, PopupMenu confirmPopup = null, Menu customMenu = null);
        Button PauseMenu_MakeSimpleButton(string name, Menu customMenu = null);
        Menu PauseMenu_MakePauseListMenu(string title);
        PopupMenu MakeTwoChoicePopup(string message, string confirmText, string cancelText);
        PopupInputMenu MakeInputFieldPopup(string message, string placeholderMessage, string confirmText, string cancelText);
        PopupMenu MakeInfoPopup(string message, string continueButtonText);
        void RegisterStartupPopup(string message);
    }
}