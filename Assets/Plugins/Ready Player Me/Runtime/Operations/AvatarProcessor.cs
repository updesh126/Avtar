﻿using System;
using UnityEngine;

namespace ReadyPlayerMe
{
    public class AvatarProcessor
    {
        private const string TAG = nameof(AvatarProcessor);
        public Action<FailureType, string> OnFailed { get; set; }

        public void ProcessAvatar(GameObject avatar, AvatarMetadata avatarMetadata)
        {
            SDKLogger.Log(TAG, "Processing avatar.");

            try
            {
                if (!avatar.transform.Find(BONE_ARMATURE))
                {
                    AddArmatureBone(avatar);
                }

                if (avatarMetadata.BodyType == BodyType.FullBody)
                {
                    SetupAnimator(avatar, avatarMetadata.OutfitGender);
                }

                RenameChildMeshes(avatar);
            }
            catch (Exception e)
            {
                var message = $"Avatar postprocess failed. {e.Message}";
                SDKLogger.Log(TAG, message);
                OnFailed?.Invoke(FailureType.AvatarProcessError, message);
            }
        }

        #region Setup Armature and Animations

        // Animation avatars
        private const string MASCULINE_ANIMATION_AVATAR_NAME = "AnimationAvatars/MasculineAnimationAvatar";
        private const string FEMININE_ANIMATION_AVATAR_NAME = "AnimationAvatars/FeminineAnimationAvatar";

        // Animation controller
        private const string ANIMATOR_CONTROLLER_NAME = "Avatar Animator";

        // Bone names
        private const string BONE_HIPS = "Hips";
        private const string BONE_ARMATURE = "Armature";


        private void AddArmatureBone(GameObject avatar)
        {
            SDKLogger.Log(TAG, "Adding armature bone");

            var armature = new GameObject();
            armature.name = BONE_ARMATURE;
            armature.transform.parent = avatar.transform;

            Transform hips = avatar.transform.Find(BONE_HIPS);
            hips.parent = armature.transform;
        }

        private void SetupAnimator(GameObject avatar, OutfitGender gender)
        {
            SDKLogger.Log(TAG, "Setting up animator");

            var animationAvatarSource = gender == OutfitGender.Masculine
                ? MASCULINE_ANIMATION_AVATAR_NAME
                : FEMININE_ANIMATION_AVATAR_NAME;
            var animationAvatar = Resources.Load<Avatar>(animationAvatarSource);
            var animatorController = Resources.Load<RuntimeAnimatorController>(ANIMATOR_CONTROLLER_NAME);

            //Added
            var usingTPC = GameObject.FindGameObjectWithTag("Player");   //Find Player to parent avatar
            if (usingTPC)  //Set Custom Process if using TPC
            {
                avatar.transform.parent = usingTPC.transform;   //Change avatar parent to PlayerArmature Prefabs
                avatar.transform.position = usingTPC.transform.position; //Change avatar position to PlayerArmature position
                avatar.transform.rotation = usingTPC.transform.rotation; //Change avatar rotation to PlayerArmature rotation
                Animator animator = avatar.GetComponentInParent<Animator>(); //Get PlayerArmature Prefabs Animator
                //RuntimeAnimatorController animatorControllerTPC = Resources.Load<RuntimeAnimatorController>("StarterAssetsThirdPerson_RPM");  // used this if you want to Load Custom Controller From Resources Folder
                //animator.runtimeAnimatorController = animatorControllerTPC; //Assign Runtime Animator if used
                animator.avatar = animationAvatar;  //Assign Animator Avatar
                animator.applyRootMotion = false;   //Set Animator Root Motion to false
                avatar.AddComponent<EyeAnimationHandler>(); //Add Ready Player Me Extra Component for Auto Blink avatar
                VoiceHandler voiceHandler = avatar.AddComponent<VoiceHandler>();    //Add Ready Player Me Extra Component Voice Handler for avatar Lipsync  
                voiceHandler.AudioSource = avatar.GetComponentInParent<AudioSource>(); ;    //Assign Audio Source for Voice Handler
            }
            else  //Back to original Ready Player Me Process if not using TPC
            {
                Animator animator = avatar.AddComponent<Animator>();
                animator.runtimeAnimatorController = animatorController;
                animator.avatar = animationAvatar;
                animator.applyRootMotion = true;
            }
            //End
        }

        #endregion

        #region Set Component Names

        // Prefix to remove from names for correction
        private const string PREFIX = "Wolf3D_";

        private const string AVATAR_PREFIX = "Avatar";
        private const string RENDERER_PREFIX = "Renderer";
        private const string MATERIAL_PREFIX = "Material";
        private const string SKINNED_MESH_PREFIX = "SkinnedMesh";


        //Texture property IDs
        private static readonly string[] ShaderProperties =
        {
            "_MainTex",
            "_BumpMap",
            "_EmissionMap",
            "_OcclusionMap",
            "_MetallicGlossMap"
        };

        /// <summary>
        ///     Name avatar assets for make them easier to view in profiler.
        ///     Naming is 'Avatar_Type_Name'
        /// </summary>
        private void RenameChildMeshes(GameObject avatar)
        {
            SkinnedMeshRenderer[] renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                var assetName = renderer.name.Replace(PREFIX, "");

                renderer.name = $"{RENDERER_PREFIX}_{assetName}";
                renderer.sharedMaterial.name = $"{MATERIAL_PREFIX}_{assetName}";
                SetTextureNames(renderer, assetName);
                SetMeshName(renderer, assetName);
            }
        }

        /// <summary>
        ///     Set a name for the texture for finding it in the Profiler.
        /// </summary>
        /// <param name="renderer">Renderer to find the texture in.</param>
        /// <param name="assetName">Name of the asset.</param>
        private void SetTextureNames(Renderer renderer, string assetName)
        {
            foreach (var propertyName in ShaderProperties)
            {
                var propertyID = Shader.PropertyToID(propertyName);

                if (renderer.sharedMaterial.HasProperty(propertyID))
                {
                    var texture = renderer.sharedMaterial.GetTexture(propertyID);
                    if (texture != null) texture.name = $"{AVATAR_PREFIX}{propertyName}_{assetName}";
                }
            }
        }

        /// <summary>
        ///     Set a name for the mesh for finding it in the Profiler.
        /// </summary>
        /// <param name="renderer">Renderer to find the mesh in.</param>
        /// <param name="assetName">Name of the asset.</param>
        private void SetMeshName(SkinnedMeshRenderer renderer, string assetName)
        {
            renderer.sharedMesh.name = $"{SKINNED_MESH_PREFIX}_{assetName}";
            renderer.updateWhenOffscreen = true;
        }

        #endregion
    }
}
