using UnityEngine;

namespace ParkourLegion.Player
{
    public class PlayerModelManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform gfxsContainer;

        private GameObject activeModel;
        private Animator activeAnimator;
        private int currentSkinId = -1;

        private const string ANIMATOR_STATE_PARAM = "state";

        public void SetModel(int skinId)
        {
            if (gfxsContainer == null)
            {
                Debug.LogError("PlayerModelManager: GFXs container is not assigned!");
                return;
            }

            int maxSkinId = gfxsContainer.childCount - 1;

            if (skinId < 0 || skinId > maxSkinId)
            {
                Debug.LogWarning($"PlayerModelManager: Invalid skinId {skinId}, defaulting to 0 (max: {maxSkinId})");
                skinId = 0;
            }

            if (currentSkinId == skinId)
            {
                return;
            }

            DisableAllModels();

            if (skinId < gfxsContainer.childCount)
            {
                activeModel = gfxsContainer.GetChild(skinId).gameObject;
                activeModel.SetActive(true);

                activeAnimator = activeModel.GetComponent<Animator>();
                if (activeAnimator == null)
                {
                    Debug.LogWarning($"PlayerModelManager: Model at index {skinId} has no Animator component");
                }

                currentSkinId = skinId;
                Debug.Log($"PlayerModelManager: Activated model {activeModel.name} (skinId: {skinId})");
            }
            else
            {
                Debug.LogError($"PlayerModelManager: skinId {skinId} exceeds available models (count: {gfxsContainer.childCount})");
            }
        }

        public void UpdateAnimation(int movementState)
        {
            if (activeAnimator == null)
            {
                return;
            }

            activeAnimator.SetInteger(ANIMATOR_STATE_PARAM, movementState);
        }

        public int GetAvailableModelCount()
        {
            if (gfxsContainer == null)
            {
                Debug.LogError("PlayerModelManager: GFXs container is not assigned!");
                return 0;
            }

            return gfxsContainer.childCount;
        }

        private void DisableAllModels()
        {
            if (gfxsContainer == null)
            {
                return;
            }

            for (int i = 0; i < gfxsContainer.childCount; i++)
            {
                gfxsContainer.GetChild(i).gameObject.SetActive(false);
            }

            activeModel = null;
            activeAnimator = null;
        }

        private void OnValidate()
        {
            if (gfxsContainer == null)
            {
                Transform gfxs = transform.Find("GFXs");
                if (gfxs != null)
                {
                    gfxsContainer = gfxs;
                }
            }
        }
    }
}
