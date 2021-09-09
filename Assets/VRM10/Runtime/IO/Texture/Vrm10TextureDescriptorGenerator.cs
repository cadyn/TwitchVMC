﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using VRMShaders;

namespace UniVRM10
{
    public sealed class Vrm10TextureDescriptorGenerator : ITextureDescriptorGenerator
    {
        private readonly GltfData m_data;
        private TextureDescriptorSet _textureDescriptorSet;

        public Vrm10TextureDescriptorGenerator(GltfData data)
        {
            m_data = data;
        }

        public TextureDescriptorSet Get()
        {
            if (_textureDescriptorSet == null)
            {
                _textureDescriptorSet = new TextureDescriptorSet();
                foreach (var (_, param) in EnumerateAllTextures(m_data))
                {
                    _textureDescriptorSet.Add(param);
                }
            }
            return _textureDescriptorSet;
        }

        /// <summary>
        /// glTF 全体で使うテクスチャーを列挙する
        /// </summary>
        private static IEnumerable<(SubAssetKey, TextureDescriptor)> EnumerateAllTextures(GltfData data)
        {
            if (!UniGLTF.Extensions.VRMC_vrm.GltfDeserializer.TryGet(data.GLTF.extensions, out UniGLTF.Extensions.VRMC_vrm.VRMC_vrm vrm))
            {
                throw new System.Exception("not vrm");
            }

            // Textures referenced by Materials.
            for (var materialIdx = 0; materialIdx < data.GLTF.materials.Count; ++materialIdx)
            {
                var m = data.GLTF.materials[materialIdx];
                if (UniGLTF.Extensions.VRMC_materials_mtoon.GltfDeserializer.TryGet(m.extensions, out var mToon))
                {
                    foreach (var (_, tex) in Vrm10MToonTextureImporter.EnumerateAllTextures(data, m, mToon))
                    {
                        yield return tex;
                    }
                }
                else
                {
                    // Fallback to glTF PBR & glTF Unlit
                    foreach (var tex in GltfPbrTextureImporter.EnumerateAllTextures(data, materialIdx))
                    {
                        yield return tex;
                    }
                }
            }

            // Thumbnail Texture referenced by VRM Meta.
            if (TryGetMetaThumbnailTextureImportParam(data, vrm, out (SubAssetKey key, TextureDescriptor) thumbnail))
            {
                yield return thumbnail;
            }
        }

        /// <summary>
        /// VRM-1 の thumbnail テクスチャー。gltf.textures ではなく gltf.images の参照であることに注意(sampler等の設定が無い)
        /// </summary>
        public static bool TryGetMetaThumbnailTextureImportParam(GltfData data, UniGLTF.Extensions.VRMC_vrm.VRMC_vrm vrm, out (SubAssetKey, TextureDescriptor) value)
        {
            if (vrm?.Meta?.ThumbnailImage == null)
            {
                value = default;
                return false;
            }

            var imageIndex = vrm.Meta.ThumbnailImage.Value;
            var gltfImage = data.GLTF.images[imageIndex];
            var name = TextureImportName.GetUnityObjectName(TextureImportTypes.sRGB, gltfImage.name, gltfImage.uri);

            GetTextureBytesAsync getThumbnailImageBytesAsync = () =>
            {
                var bytes = data.GLTF.GetImageBytes(data.Storage, imageIndex);
                return Task.FromResult(GltfTextureImporter.ToArray(bytes));
            };
            var texDesc = new TextureDescriptor(name, gltfImage.GetExt(), gltfImage.uri, Vector2.zero, Vector2.one, default, TextureImportTypes.sRGB, default, default,
               getThumbnailImageBytesAsync, default, default,
               default, default, default
               );
            value = (texDesc.SubAssetKey, texDesc);
            return true;
        }
    }
}
