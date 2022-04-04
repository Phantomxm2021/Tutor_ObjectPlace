using System;
using UnityEngine;
using System.Collections;
using com.Phantoms.ARMODAPI.Runtime;
using com.Phantoms.ActionNotification.Runtime;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace Tutor_ObjectPlace
{
    public class Tutor_ObjectPlaceMainEntry
    {
        //ARMOD API. Dont New ARMODAPI in here       
        internal static readonly API ARMODAPI = new API(nameof(Tutor_ObjectPlace));

        private const string CONST_FIND_NODE = "Find";
        private const string CONST_FOUND_NODE = "Found";
        private const string CONST_UI_CANVAS = "UICanvas";
        private const string CONST_FOCUS_GROUP = "FocusGroup";
        private const string CONST_PLACEMENT_BTN = "PlacementBtn";
        private const string CONST_AR_VIRTUAL_OBJECT = "VirtualObject";

        private static readonly int PLACED = Animator.StringToHash("Placed");
        private static readonly int PLACING_STAGE = Animator.StringToHash("PlacingStage");
        private static readonly int FIND_PLANE_STAGE = Animator.StringToHash("FindPlaneStage");

        private bool placed;
        private UIStageEnum uiStage;

        private GameObject findNode;
        private GameObject uiCanvas;
        private GameObject foundNode;
        private GameObject focusGroup;
        private GameObject virtualObject;

        private Transform focusGroupTrans;

        private Animator userinterfaceAnimator;

        public enum UIStageEnum
        {
            None,
            Find,
            Placing,
            Placed,
        }

        public async void OnLoad()
        {
            //Use this for initialization. Please delete the function if it is not used

            //1st. loading our assets
            var tmp_UICanvasPrefab = await ARMODAPI.LoadAssetAsync<GameObject>(CONST_UI_CANVAS);
            uiCanvas = UnityEngine.Object.Instantiate(tmp_UICanvasPrefab);

            var tmp_FocusGroupPrefab = await ARMODAPI.LoadAssetAsync<GameObject>(CONST_FOCUS_GROUP);
            focusGroup = UnityEngine.Object.Instantiate(tmp_FocusGroupPrefab);
            focusGroupTrans = focusGroup.transform;
            RuntimeTagManager.GetRuntimeTagManager.GetGameObjectByTag(CONST_FIND_NODE, out findNode);
            RuntimeTagManager.GetRuntimeTagManager.GetGameObjectByTag(CONST_FOUND_NODE, out foundNode);
            SetFocusVisualizer(false,false);
            Assert.IsNotNull(findNode);
            Assert.IsNotNull(foundNode);
            
            var tmp_VirtualObjectPrefab = await ARMODAPI.LoadAssetAsync<GameObject>(CONST_AR_VIRTUAL_OBJECT);
            Assert.IsNotNull(tmp_VirtualObjectPrefab);
            virtualObject = UnityEngine.Object.Instantiate(tmp_VirtualObjectPrefab);

            //2ed. Dont render AR virtual object before placing
            virtualObject.SetActive(false);

            //3rd. Register callback event for User interface
            userinterfaceAnimator = uiCanvas.GetComponent<Animator>();
            RuntimeTagManager.GetRuntimeTagManager.GetGameObjectByTag(CONST_PLACEMENT_BTN, out var tmp_PlacementGo);
           
            tmp_PlacementGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                //Move our virtual object to destination
                virtualObject.transform.SetPositionAndRotation(focusGroupTrans.position, focusGroupTrans.rotation);
                virtualObject.SetActive(true);
            
                //Hide placing UI view
                userinterfaceAnimator.SetTrigger(PLACED);
                uiStage = UIStageEnum.Placed;
            
                //Hide focus visualizer group
                focusGroup.SetActive(false);
            
                //Dont run `OnEvent` logic again
                placed = true;
            });
        }

        public void OnEvent(BaseNotificationData _data)
        {
            //General event callback. Please delete the function if it is not used
            if (placed) return;
            if (!userinterfaceAnimator) return;
            if (findNode == null || foundNode == null || focusGroup == null) return;
            if (!(_data is FocusResultNotificationData tmp_Data)) return;

            switch (tmp_Data.FocusState)
            {
                case FindingType.Finding:
                    SetFocusVisualizer(false, true);
                    if (uiStage == UIStageEnum.None || uiStage == UIStageEnum.Placing)
                    {
                        userinterfaceAnimator.SetTrigger(FIND_PLANE_STAGE);
                        uiStage = UIStageEnum.Find;
                    }

                    break;
                case FindingType.Found:
                    SetFocusVisualizer(true, false);
                    if (uiStage == UIStageEnum.Find)
                    {
                        userinterfaceAnimator.SetTrigger(PLACING_STAGE);
                        uiStage = UIStageEnum.Placing;
                    }

                    break;
                case FindingType.Limit:
                    if (focusGroup.activeSelf)
                        focusGroup.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            focusGroupTrans.SetPositionAndRotation(tmp_Data.FocusPos, tmp_Data.FocusRot);
        }

        private void SetFocusVisualizer(bool _found, bool _find)
        {
            if (!focusGroup.activeSelf)
                focusGroup.SetActive(true);

            if (foundNode.activeSelf != _found)
                foundNode.SetActive(_found);

            if (findNode.activeSelf != _find)
                findNode.SetActive(_find);
        }


        public void ReleaseMemory()
        {
            //Release Memory after AR close. Please delete the function if it is not used
            UnityEngine.Object.Destroy(virtualObject);
            UnityEngine.Object.Destroy(uiCanvas);
            UnityEngine.Object.Destroy(focusGroup);
        }
    }
}