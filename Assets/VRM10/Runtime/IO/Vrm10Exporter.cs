using System;
using System.Collections.Generic;
using System.Linq;
using UniGLTF;
using UnityEngine;
using VrmLib;
using VRMShaders;


namespace UniVRM10
{
    public class Vrm10Exporter : IDisposable
    {
        public readonly Vrm10Storage Storage = new Vrm10Storage();

        public readonly string VrmExtensionName = "VRMC_vrm";

        ITextureSerializer m_textureSerializer;
        TextureExporter m_textureExporter;

        GltfExportSettings m_settings;

        public Vrm10Exporter(ITextureSerializer textureSerializer, GltfExportSettings settings)
        {
            m_settings = settings;

            if (textureSerializer == null)
            {
                throw new ArgumentException(nameof(textureSerializer));
            }

            Storage.Gltf.extensionsUsed.Add(glTF_KHR_materials_unlit.ExtensionName);
            Storage.Gltf.extensionsUsed.Add(glTF_KHR_texture_transform.ExtensionName);
            Storage.Gltf.extensionsUsed.Add(UniGLTF.Extensions.VRMC_vrm.VRMC_vrm.ExtensionName);
            Storage.Gltf.extensionsUsed.Add(UniGLTF.Extensions.VRMC_materials_mtoon.VRMC_materials_mtoon.ExtensionName);
            Storage.Gltf.extensionsUsed.Add(UniGLTF.Extensions.VRMC_springBone.VRMC_springBone.ExtensionName);
            Storage.Gltf.extensionsUsed.Add(UniGLTF.Extensions.VRMC_node_constraint.VRMC_node_constraint.ExtensionName);
            Storage.Gltf.buffers.Add(new glTFBuffer
            {

            });

            m_textureSerializer = textureSerializer;
            m_textureExporter = new TextureExporter(m_textureSerializer);
        }

        public void Dispose()
        {
            m_textureExporter.Dispose();
        }

        public void ExportAsset(Model model)
        {
            Storage.Gltf.asset = new glTFAssets
            {
            };
            if (!string.IsNullOrEmpty(model.AssetVersion)) Storage.Gltf.asset.version = model.AssetVersion;
            if (!string.IsNullOrEmpty(model.AssetMinVersion)) Storage.Gltf.asset.minVersion = model.AssetMinVersion;

            if (!string.IsNullOrEmpty(model.AssetGenerator)) Storage.Gltf.asset.generator = model.AssetGenerator;

            if (!string.IsNullOrEmpty(model.AssetCopyright)) Storage.Gltf.asset.copyright = model.AssetCopyright;
        }

        public void Reserve(int bytesLength)
        {
            Storage.Reserve(bytesLength);
        }

        public void ExportMeshes(List<MeshGroup> groups, List<object> materials, ExportArgs option)
        {
            foreach (var group in groups)
            {
                var mesh = group.ExportMeshGroup(materials, Storage, option);
                Storage.Gltf.meshes.Add(mesh);
            }
        }

        public void ExportNodes(Node root, List<Node> nodes, List<MeshGroup> groups, ExportArgs option)
        {
            foreach (var x in nodes)
            {
                var node = new glTFNode
                {
                    name = x.Name,
                };

                node.translation = x.LocalTranslation.ToFloat3();
                node.rotation = x.LocalRotation.ToFloat4();
                node.scale = x.LocalScaling.ToFloat3();

                if (x.MeshGroup != null)
                {
                    node.mesh = groups.IndexOfThrow(x.MeshGroup);
                    var skin = x.MeshGroup.Skin;
                    if (skin != null)
                    {
                        var skinIndex = Storage.Gltf.skins.Count;
                        var gltfSkin = new glTFSkin()
                        {
                            joints = skin.Joints.Select(joint => nodes.IndexOfThrow(joint)).ToArray()
                        };
                        if (skin.InverseMatrices == null)
                        {
                            skin.CalcInverseMatrices();
                        }
                        if (skin.InverseMatrices != null)
                        {
                            gltfSkin.inverseBindMatrices = skin.InverseMatrices.AddAccessorTo(Storage, 0, option.sparse);
                        }
                        if (skin.Root != null)
                        {
                            gltfSkin.skeleton = nodes.IndexOf(skin.Root);
                        }
                        Storage.Gltf.skins.Add(gltfSkin);
                        node.skin = skinIndex;
                    }
                }

                node.children = x.Children.Select(child => nodes.IndexOfThrow(child)).ToArray();

                Storage.Gltf.nodes.Add(node);
            }

            Storage.Gltf.scenes.Add(new gltfScene()
            {
                nodes = root.Children.Select(child => nodes.IndexOfThrow(child)).ToArray()
            });
        }

        /// <summary>
        /// revere X
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static float[] ReverseX(Vector3 v)
        {
            return new float[] { -v.x, v.y, v.z };
        }

        public void Export(GameObject root, Model model, ModelExporter converter, ExportArgs option, VRM10ObjectMeta vrmMeta = null)
        {
            ExportAsset(model);

            ///
            /// 必要な容量を先に確保
            /// (sparseは考慮してないので大きめ)
            ///
            {
                var reserveBytes = 0;
                // mesh
                foreach (var g in model.MeshGroups)
                {
                    foreach (var mesh in g.Meshes)
                    {
                        // 頂点バッファ
                        reserveBytes += mesh.IndexBuffer.ByteLength;
                        foreach (var kv in mesh.VertexBuffer)
                        {
                            reserveBytes += kv.Value.ByteLength;
                        }
                        // morph
                        foreach (var morph in mesh.MorphTargets)
                        {
                            foreach (var kv in morph.VertexBuffer)
                            {
                                reserveBytes += kv.Value.ByteLength;
                            }
                        }
                    }
                }
                Reserve(reserveBytes);
            }

            // material
            var materialExporter = new Vrm10MaterialExporter();
            foreach (Material material in model.Materials)
            {
                var glTFMaterial = materialExporter.ExportMaterial(material, m_textureExporter, m_settings);
                Storage.Gltf.materials.Add(glTFMaterial);
            }

            // mesh
            ExportMeshes(model.MeshGroups, model.Materials, option);

            // node
            ExportNodes(model.Root, model.Nodes, model.MeshGroups, option);

            var (vrm, vrmSpringBone, thumbnailTextureIndex) = ExportVrm(root, model, converter, vrmMeta);

            // Extension で Texture が増える場合があるので最後に呼ぶ
            var exportedTextures = m_textureExporter.Export();
            for (var exportedTextureIdx = 0; exportedTextureIdx < exportedTextures.Count; ++exportedTextureIdx)
            {
                var (unityTexture, texColorSpace) = exportedTextures[exportedTextureIdx];
                Storage.Gltf.PushGltfTexture(0, unityTexture, texColorSpace, m_textureSerializer);
            }

            if (thumbnailTextureIndex.HasValue)
            {
                vrm.Meta.ThumbnailImage = Storage.Gltf.textures[thumbnailTextureIndex.Value].source;
            }

            UniGLTF.Extensions.VRMC_vrm.GltfSerializer.SerializeTo(ref Storage.Gltf.extensions, vrm);

            if (vrmSpringBone != null)
            {
                UniGLTF.Extensions.VRMC_springBone.GltfSerializer.SerializeTo(ref Storage.Gltf.extensions, vrmSpringBone);
            }

            // Fix Duplicated name
            gltfExporter.FixName(Storage.Gltf);
        }

        /// <summary>
        /// VRMコンポーネントのエクスポート
        /// </summary>
        /// <param name="vrm"></param>
        /// <param name="springBone"></param>
        /// <param name="constraint"></param>
        /// <param name="root"></param>
        /// <param name="model"></param>
        /// <param name="converter"></param>
        /// <param name="vrmObject"></param>
        /// <returns></returns>
        (UniGLTF.Extensions.VRMC_vrm.VRMC_vrm vrm,
        UniGLTF.Extensions.VRMC_springBone.VRMC_springBone springBone,
        int? thumbnailIndex) ExportVrm(GameObject root, Model model, ModelExporter converter, VRM10ObjectMeta vrmMeta)
        {
            var vrmController = root?.GetComponent<VRM10Controller>();

            if (vrmMeta == null)
            {
                if (vrmController?.Vrm?.Meta == null)
                {
                    throw new NullReferenceException("metaObject is null");
                }
                vrmMeta = vrmController.Vrm.Meta;
            }

            var vrm = new UniGLTF.Extensions.VRMC_vrm.VRMC_vrm
            {
                Humanoid = new UniGLTF.Extensions.VRMC_vrm.Humanoid
                {
                    HumanBones = new UniGLTF.Extensions.VRMC_vrm.HumanBones
                    {
                    },
                },
                Meta = new UniGLTF.Extensions.VRMC_vrm.Meta
                {
                    AllowExcessivelySexualUsage = false,
                    AllowExcessivelyViolentUsage = false,
                    AllowPoliticalOrReligiousUsage = false,
                    AllowRedistribution = false,
                },
            };

            //
            // required
            //
            ExportHumanoid(vrm, model);
            var thumbnailTextureIndex = ExportMeta(vrm, vrmMeta);

            //
            // optional
            //
            UniGLTF.Extensions.VRMC_springBone.VRMC_springBone vrmSpringBone = default;
            if (vrmController != null)
            {
                ExportExpression(vrm, vrmController, model, converter);
                ExportLookAt(vrm, vrmController);
                ExportFirstPerson(vrm, vrmController, model, converter);

                vrmSpringBone = ExportSpringBone(vrmController, model, converter);
                ExportConstraints(vrmController, model, converter);
            }

            return (vrm, vrmSpringBone, thumbnailTextureIndex);
        }

        UniGLTF.Extensions.VRMC_springBone.ColliderShape ExportShape(VRM10SpringBoneCollider z)
        {
            var shape = new UniGLTF.Extensions.VRMC_springBone.ColliderShape();
            switch (z.ColliderType)
            {
                case VRM10SpringBoneColliderTypes.Sphere:
                    {
                        shape.Sphere = new UniGLTF.Extensions.VRMC_springBone.ColliderShapeSphere
                        {
                            Radius = z.Radius,
                            Offset = ReverseX(z.Offset),
                        };
                        break;
                    }

                case VRM10SpringBoneColliderTypes.Capsule:
                    {
                        shape.Capsule = new UniGLTF.Extensions.VRMC_springBone.ColliderShapeCapsule
                        {
                            Radius = z.Radius,
                            Offset = new float[] { z.Offset.x, z.Offset.y, z.Offset.z },
                            Tail = new float[] { z.Tail.x, z.Tail.y, z.Tail.z },
                        };
                        break;
                    }
            }
            return shape;
        }

        UniGLTF.Extensions.VRMC_springBone.SpringBoneJoint ExportJoint(VRM10SpringBoneJoint y, Func<Transform, int> getIndexFromTransform)
        {
            var joint = new UniGLTF.Extensions.VRMC_springBone.SpringBoneJoint
            {
                Node = getIndexFromTransform(y.transform),
                HitRadius = y.m_jointRadius,
                DragForce = y.m_dragForce,
                Stiffness = y.m_stiffnessForce,
                GravityDir = ReverseX(y.m_gravityDir),
                GravityPower = y.m_gravityPower,
            };
            return joint;
        }

        UniGLTF.Extensions.VRMC_springBone.VRMC_springBone ExportSpringBone(VRM10Controller controller, Model model, ModelExporter converter)
        {
            var springBone = new UniGLTF.Extensions.VRMC_springBone.VRMC_springBone
            {
                Colliders = new List<UniGLTF.Extensions.VRMC_springBone.Collider>(),
                ColliderGroups = new List<UniGLTF.Extensions.VRMC_springBone.ColliderGroup>(),
                Springs = new List<UniGLTF.Extensions.VRMC_springBone.Spring>(),
            };

            // colliders
            Func<Transform, int> getNodeIndexFromTransform = t =>
            {
                var node = converter.Nodes[t.gameObject];
                return model.Nodes.IndexOf(node);
            };

            var colliders = controller.GetComponentsInChildren<VRM10SpringBoneCollider>();
            foreach (var c in colliders)
            {
                springBone.Colliders.Add(new UniGLTF.Extensions.VRMC_springBone.Collider
                {
                    Node = getNodeIndexFromTransform(c.transform),
                    Shape = ExportShape(c),
                });
            }

            // colliderGroups
            foreach (var x in controller.SpringBone.ColliderGroups)
            {
                springBone.ColliderGroups.Add(new UniGLTF.Extensions.VRMC_springBone.ColliderGroup
                {
                    Colliders = x.Colliders.Select(y => Array.IndexOf(colliders, y)).ToArray(),
                });
            }

            // springs
            foreach (var x in controller.SpringBone.Springs)
            {
                var spring = new UniGLTF.Extensions.VRMC_springBone.Spring
                {
                    Name = x.Name,
                    Joints = x.Joints.Select(y => ExportJoint(y, getNodeIndexFromTransform)).ToList(),
                    ColliderGroups = x.ColliderGroups.Select(y => controller.SpringBone.ColliderGroups.IndexOf(y)).ToArray(),
                };
                springBone.Springs.Add(spring);
            }

            return springBone;
        }

        void ExportConstraints(VRM10Controller vrmController, Model model, ModelExporter converter)
        {
            var constraints = vrmController.GetComponentsInChildren<VRM10Constraint>();
            foreach (var constraint in constraints)
            {
                UniGLTF.Extensions.VRMC_node_constraint.VRMC_node_constraint vrmConstraint = default;
                switch (constraint)
                {
                    case VRM10PositionConstraint positionConstraint:
                        vrmConstraint = ExportPostionConstraint(positionConstraint, model, converter);
                        break;

                    case VRM10RotationConstraint rotationConstraint:
                        vrmConstraint = ExportRotationConstraint(rotationConstraint, model, converter);
                        break;

                    case VRM10AimConstraint aimConstraint:
                        vrmConstraint = ExportAimConstraint(aimConstraint, model, converter);
                        break;

                    default:
                        throw new NotImplementedException();
                }

                // serialize to gltfNode
                var node = converter.Nodes[constraint.gameObject];
                var nodeIndex = model.Nodes.IndexOf(node);
                var gltfNode = Storage.Gltf.nodes[nodeIndex];
                UniGLTF.Extensions.VRMC_node_constraint.GltfSerializer.SerializeTo(ref gltfNode.extensions, vrmConstraint);
            }
        }

        static bool[] ToArray(AxisMask mask)
        {
            return new bool[]
            {
                mask.HasFlag(AxisMask.X),
                mask.HasFlag(AxisMask.Y),
                mask.HasFlag(AxisMask.Z),
            };
        }

        static UniGLTF.Extensions.VRMC_node_constraint.VRMC_node_constraint ExportPostionConstraint(VRM10PositionConstraint c, Model model, ModelExporter converter)
        {
            return new UniGLTF.Extensions.VRMC_node_constraint.VRMC_node_constraint
            {
                Constraint = new UniGLTF.Extensions.VRMC_node_constraint.Constraint
                {
                    Position = new UniGLTF.Extensions.VRMC_node_constraint.PositionConstraint
                    {
                        Source = model.Nodes.IndexOf(converter.Nodes[c.Source.gameObject]),
                        SourceSpace = c.SourceCoordinate,
                        DestinationSpace = c.DestinationCoordinate,
                        FreezeAxes = ToArray(c.FreezeAxes),
                        Weight = c.Weight,
                    }
                },
            };
        }

        static UniGLTF.Extensions.VRMC_node_constraint.VRMC_node_constraint ExportRotationConstraint(VRM10RotationConstraint c, Model model, ModelExporter converter)
        {
            return new UniGLTF.Extensions.VRMC_node_constraint.VRMC_node_constraint
            {
                Constraint = new UniGLTF.Extensions.VRMC_node_constraint.Constraint
                {
                    Rotation = new UniGLTF.Extensions.VRMC_node_constraint.RotationConstraint
                    {
                        Source = model.Nodes.IndexOf(converter.Nodes[c.Source.gameObject]),
                        SourceSpace = c.SourceCoordinate,
                        DestinationSpace = c.DestinationCoordinate,
                        FreezeAxes = ToArray(c.FreezeAxes),
                        Weight = c.Weight,
                    },
                },
            };
        }

        static UniGLTF.Extensions.VRMC_node_constraint.VRMC_node_constraint ExportAimConstraint(VRM10AimConstraint c, Model model, ModelExporter converter)
        {
            return new UniGLTF.Extensions.VRMC_node_constraint.VRMC_node_constraint
            {
                Constraint = new UniGLTF.Extensions.VRMC_node_constraint.Constraint
                {
                    Aim = new UniGLTF.Extensions.VRMC_node_constraint.AimConstraint
                    {
                        Source = model.Nodes.IndexOf(converter.Nodes[c.Source.gameObject]),
                        // AimVector = ReverseX(c.AimVector),
                        // UpVector = ReverseX(c.UpVector),
                        Weight = c.Weight,
                    },
                },
            };
        }

        static UniGLTF.Extensions.VRMC_vrm.MeshAnnotation ExportMeshAnnotation(RendererFirstPersonFlags flags, Transform root, Func<Renderer, int> getIndex)
        {
            return new UniGLTF.Extensions.VRMC_vrm.MeshAnnotation
            {
                Node = getIndex(flags.GetRenderer(root)),
                Type = flags.FirstPersonFlag,
            };
        }

        void ExportFirstPerson(UniGLTF.Extensions.VRMC_vrm.VRMC_vrm vrm, VRM10Controller vrmController, Model model, ModelExporter converter)
        {
            if (!(vrmController?.Vrm?.FirstPerson is VRM10ObjectFirstPerson firstPerson))
            {
                return;
            }

            vrm.FirstPerson = new UniGLTF.Extensions.VRMC_vrm.FirstPerson
            {
                MeshAnnotations = new List<UniGLTF.Extensions.VRMC_vrm.MeshAnnotation>(),
            };
            Func<Renderer, int> getIndex = r =>
            {
                var node = converter.Nodes[r.gameObject];
                return model.Nodes.IndexOf(node);
            };
            foreach (var f in firstPerson.Renderers)
            {
                vrm.FirstPerson.MeshAnnotations.Add(ExportMeshAnnotation(f, vrmController.transform, getIndex));
            }
        }

        UniGLTF.Extensions.VRMC_vrm.LookAtRangeMap ExportLookAtRangeMap(CurveMapper mapper)
        {
            return new UniGLTF.Extensions.VRMC_vrm.LookAtRangeMap
            {
                InputMaxValue = mapper.CurveXRangeDegree,
                OutputScale = mapper.CurveYRangeDegree,
            };
        }

        void ExportLookAt(UniGLTF.Extensions.VRMC_vrm.VRMC_vrm vrm, VRM10Controller vrmController)
        {
            if (!(vrmController?.Vrm?.LookAt is VRM10ObjectLookAt lookAt))
            {
                return;
            }

            vrm.LookAt = new UniGLTF.Extensions.VRMC_vrm.LookAt
            {
                Type = lookAt.LookAtType,
                OffsetFromHeadBone = new float[]{
                    lookAt.OffsetFromHead.x ,
                    lookAt.OffsetFromHead.y ,
                    lookAt.OffsetFromHead.z ,
                },
                RangeMapHorizontalInner = ExportLookAtRangeMap(lookAt.HorizontalInner),
                RangeMapHorizontalOuter = ExportLookAtRangeMap(lookAt.HorizontalOuter),
                RangeMapVerticalDown = ExportLookAtRangeMap(lookAt.VerticalDown),
                RangeMapVerticalUp = ExportLookAtRangeMap(lookAt.VerticalUp),
            };
        }

        UniGLTF.Extensions.VRMC_vrm.MorphTargetBind ExportMorphTargetBinding(MorphTargetBinding binding, Func<string, int> getIndex)
        {
            return new UniGLTF.Extensions.VRMC_vrm.MorphTargetBind
            {
                Node = getIndex(binding.RelativePath),
                Index = binding.Index,
                Weight = binding.Weight,
            };
        }

        UniGLTF.Extensions.VRMC_vrm.MaterialColorBind ExportMaterialColorBinding(MaterialColorBinding binding, Func<string, int> getIndex)
        {
            return new UniGLTF.Extensions.VRMC_vrm.MaterialColorBind
            {
                Material = getIndex(binding.MaterialName),
                Type = binding.BindType,
                TargetValue = new float[] { binding.TargetValue.x, binding.TargetValue.y, binding.TargetValue.z, binding.TargetValue.w },
            };
        }

        UniGLTF.Extensions.VRMC_vrm.TextureTransformBind ExportTextureTransformBinding(MaterialUVBinding binding, Func<string, int> getIndex)
        {
            return new UniGLTF.Extensions.VRMC_vrm.TextureTransformBind
            {
                Material = getIndex(binding.MaterialName),
                Offset = new float[] { binding.Offset.x, binding.Offset.y },
                Scale = new float[] { binding.Scaling.x, binding.Scaling.y },
            };
        }

        UniGLTF.Extensions.VRMC_vrm.Expression ExportExpression(VRM10Expression e, VRM10Controller vrmController, Model model, ModelExporter converter)
        {
            if (e == null)
            {
                return null;
            }

            Func<string, int> getIndexFromRelativePath = relativePath =>
            {
                var rendererNode = vrmController.transform.GetFromPath(relativePath);
                var node = converter.Nodes[rendererNode.gameObject];
                return model.Nodes.IndexOf(node);
            };

            var vrmExpression = new UniGLTF.Extensions.VRMC_vrm.Expression
            {
                // Preset = e.Preset,
                // Name = e.ExpressionName,
                IsBinary = e.IsBinary,
                OverrideBlink = e.OverrideBlink,
                OverrideLookAt = e.OverrideLookAt,
                OverrideMouth = e.OverrideMouth,
                MorphTargetBinds = new List<UniGLTF.Extensions.VRMC_vrm.MorphTargetBind>(),
                MaterialColorBinds = new List<UniGLTF.Extensions.VRMC_vrm.MaterialColorBind>(),
                TextureTransformBinds = new List<UniGLTF.Extensions.VRMC_vrm.TextureTransformBind>(),
            };
            Func<string, int> getIndexFromMaterialName = materialName =>
            {
                for (int i = 0; i < model.Materials.Count; ++i)
                {
                    var m = model.Materials[i] as Material;
                    if (m.name == materialName)
                    {
                        return i;
                    }
                }
                throw new KeyNotFoundException();
            };

            foreach (var b in e.MorphTargetBindings)
            {
                try
                {
                    vrmExpression.MorphTargetBinds.Add(ExportMorphTargetBinding(b, getIndexFromRelativePath));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(ex);
                }
            }
            foreach (var b in e.MaterialColorBindings)
            {
                try
                {
                    vrmExpression.MaterialColorBinds.Add(ExportMaterialColorBinding(b, getIndexFromMaterialName));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(ex);
                }
            }
            foreach (var b in e.MaterialUVBindings)
            {
                try
                {
                    vrmExpression.TextureTransformBinds.Add(ExportTextureTransformBinding(b, getIndexFromMaterialName));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(ex);
                }
            }
            return vrmExpression;
        }

        void ExportExpression(UniGLTF.Extensions.VRMC_vrm.VRMC_vrm vrm, VRM10Controller vrmController, Model model, ModelExporter converter)
        {
            if (vrmController?.Vrm?.Expression?.Clips == null)
            {
                return;
            }

            vrm.Expressions = new UniGLTF.Extensions.VRMC_vrm.Expressions
            {
                Preset = new UniGLTF.Extensions.VRMC_vrm.Preset
                {
                    Happy = ExportExpression(vrmController.Vrm.Expression.Happy, vrmController, model, converter),
                    Angry = ExportExpression(vrmController.Vrm.Expression.Angry, vrmController, model, converter),
                    Sad = ExportExpression(vrmController.Vrm.Expression.Sad, vrmController, model, converter),
                    Relaxed = ExportExpression(vrmController.Vrm.Expression.Relaxed, vrmController, model, converter),
                    Surprised = ExportExpression(vrmController.Vrm.Expression.Surprised, vrmController, model, converter),
                    Aa = ExportExpression(vrmController.Vrm.Expression.Aa, vrmController, model, converter),
                    Ih = ExportExpression(vrmController.Vrm.Expression.Ih, vrmController, model, converter),
                    Ou = ExportExpression(vrmController.Vrm.Expression.Ou, vrmController, model, converter),
                    Ee = ExportExpression(vrmController.Vrm.Expression.Ee, vrmController, model, converter),
                    Oh = ExportExpression(vrmController.Vrm.Expression.Oh, vrmController, model, converter),
                    Blink = ExportExpression(vrmController.Vrm.Expression.Blink, vrmController, model, converter),
                    BlinkLeft = ExportExpression(vrmController.Vrm.Expression.BlinkLeft, vrmController, model, converter),
                    BlinkRight = ExportExpression(vrmController.Vrm.Expression.BlinkRight, vrmController, model, converter),
                    LookUp = ExportExpression(vrmController.Vrm.Expression.LookUp, vrmController, model, converter),
                    LookDown = ExportExpression(vrmController.Vrm.Expression.LookDown, vrmController, model, converter),
                    LookLeft = ExportExpression(vrmController.Vrm.Expression.LookLeft, vrmController, model, converter),
                    LookRight = ExportExpression(vrmController.Vrm.Expression.LookRight, vrmController, model, converter),
                },
                Custom = vrmController.Vrm.Expression.CustomClips.ToDictionary(c => c.name, c => ExportExpression(c, vrmController, model, converter)),
            };
        }

        int? ExportMeta(UniGLTF.Extensions.VRMC_vrm.VRMC_vrm vrm, VRM10ObjectMeta meta)
        {
            vrm.Meta.Name = meta.Name;
            vrm.Meta.Version = meta.Version;
            vrm.Meta.Authors = meta.Authors.ToList();
            vrm.Meta.CopyrightInformation = meta.CopyrightInformation;
            vrm.Meta.ContactInformation = meta.ContactInformation;
            vrm.Meta.References = meta.References.ToList();
            // vrm.Meta.ThirdPartyLicenses =
            vrm.Meta.AvatarPermission = meta.AllowedUser;
            vrm.Meta.AllowExcessivelyViolentUsage = meta.ViolentUsage;
            vrm.Meta.AllowExcessivelySexualUsage = meta.SexualUsage;
            vrm.Meta.CommercialUsage = meta.CommercialUsage;
            vrm.Meta.AllowPoliticalOrReligiousUsage = meta.PoliticalOrReligiousUsage;
            vrm.Meta.CreditNotation = meta.CreditNotation;
            vrm.Meta.AllowRedistribution = meta.Redistribution;
            vrm.Meta.Modification = meta.ModificationLicense;
            vrm.Meta.OtherLicenseUrl = meta.OtherLicenseUrl;
            int? thumbnailTextureIndex = default;
            if (meta.Thumbnail != null)
            {
                thumbnailTextureIndex = m_textureExporter.RegisterExportingAsSRgb(meta.Thumbnail, needsAlpha: true);
            }
            return thumbnailTextureIndex;
        }

        void ExportHumanoid(UniGLTF.Extensions.VRMC_vrm.VRMC_vrm vrm, Model model)
        {
            // humanoid
            for (int i = 0; i < model.Nodes.Count; ++i)
            {
                var bone = model.Nodes[i];
                switch (bone.HumanoidBone)
                {
                    case HumanoidBones.hips: vrm.Humanoid.HumanBones.Hips = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;
                    case HumanoidBones.spine: vrm.Humanoid.HumanBones.Spine = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.chest: vrm.Humanoid.HumanBones.Chest = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.upperChest: vrm.Humanoid.HumanBones.UpperChest = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.neck: vrm.Humanoid.HumanBones.Neck = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.head: vrm.Humanoid.HumanBones.Head = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftEye: vrm.Humanoid.HumanBones.LeftEye = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightEye: vrm.Humanoid.HumanBones.RightEye = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.jaw: vrm.Humanoid.HumanBones.Jaw = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftUpperLeg: vrm.Humanoid.HumanBones.LeftUpperLeg = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftLowerLeg: vrm.Humanoid.HumanBones.LeftLowerLeg = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftFoot: vrm.Humanoid.HumanBones.LeftFoot = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftToes: vrm.Humanoid.HumanBones.LeftToes = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightUpperLeg: vrm.Humanoid.HumanBones.RightUpperLeg = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightLowerLeg: vrm.Humanoid.HumanBones.RightLowerLeg = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightFoot: vrm.Humanoid.HumanBones.RightFoot = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightToes: vrm.Humanoid.HumanBones.RightToes = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftShoulder: vrm.Humanoid.HumanBones.LeftShoulder = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftUpperArm: vrm.Humanoid.HumanBones.LeftUpperArm = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftLowerArm: vrm.Humanoid.HumanBones.LeftLowerArm = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftHand: vrm.Humanoid.HumanBones.LeftHand = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightShoulder: vrm.Humanoid.HumanBones.RightShoulder = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightUpperArm: vrm.Humanoid.HumanBones.RightUpperArm = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightLowerArm: vrm.Humanoid.HumanBones.RightLowerArm = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightHand: vrm.Humanoid.HumanBones.RightHand = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftThumbProximal: vrm.Humanoid.HumanBones.LeftThumbProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftThumbIntermediate: vrm.Humanoid.HumanBones.LeftThumbIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftThumbDistal: vrm.Humanoid.HumanBones.LeftThumbDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftIndexProximal: vrm.Humanoid.HumanBones.LeftIndexProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftIndexIntermediate: vrm.Humanoid.HumanBones.LeftIndexIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftIndexDistal: vrm.Humanoid.HumanBones.LeftIndexDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftMiddleProximal: vrm.Humanoid.HumanBones.LeftMiddleProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftMiddleIntermediate: vrm.Humanoid.HumanBones.LeftMiddleIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftMiddleDistal: vrm.Humanoid.HumanBones.LeftMiddleDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftRingProximal: vrm.Humanoid.HumanBones.LeftRingProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftRingIntermediate: vrm.Humanoid.HumanBones.LeftRingIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftRingDistal: vrm.Humanoid.HumanBones.LeftRingDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftLittleProximal: vrm.Humanoid.HumanBones.LeftLittleProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftLittleIntermediate: vrm.Humanoid.HumanBones.LeftLittleIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.leftLittleDistal: vrm.Humanoid.HumanBones.LeftLittleDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightThumbProximal: vrm.Humanoid.HumanBones.RightThumbProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightThumbIntermediate: vrm.Humanoid.HumanBones.RightThumbIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightThumbDistal: vrm.Humanoid.HumanBones.RightThumbDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightIndexProximal: vrm.Humanoid.HumanBones.RightIndexProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightIndexIntermediate: vrm.Humanoid.HumanBones.RightIndexIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightIndexDistal: vrm.Humanoid.HumanBones.RightIndexDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightMiddleProximal: vrm.Humanoid.HumanBones.RightMiddleProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightMiddleIntermediate: vrm.Humanoid.HumanBones.RightMiddleIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightMiddleDistal: vrm.Humanoid.HumanBones.RightMiddleDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightRingProximal: vrm.Humanoid.HumanBones.RightRingProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightRingIntermediate: vrm.Humanoid.HumanBones.RightRingIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightRingDistal: vrm.Humanoid.HumanBones.RightRingDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightLittleProximal: vrm.Humanoid.HumanBones.RightLittleProximal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightLittleIntermediate: vrm.Humanoid.HumanBones.RightLittleIntermediate = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;

                    case HumanoidBones.rightLittleDistal: vrm.Humanoid.HumanBones.RightLittleDistal = new UniGLTF.Extensions.VRMC_vrm.HumanBone { Node = i }; break;
                }
            }
        }

        /// <summary>
        /// 便利関数
        /// </summary>
        /// <param name="go"></param>
        /// <param name="getTextureBytes"></param>
        /// <returns></returns>
        public static byte[] Export(GameObject go, ITextureSerializer textureSerializer = null)
        {
            // ヒエラルキーからジオメトリーを収集
            var converter = new UniVRM10.ModelExporter();
            var model = converter.Export(go);

            // 右手系に変換
            VrmLib.ModelExtensionsForCoordinates.ConvertCoordinate(model, VrmLib.Coordinates.Vrm1);

            // Model と go から VRM-1.0 にExport
            var exporter10 = new Vrm10Exporter(textureSerializer ?? new RuntimeTextureSerializer(), new GltfExportSettings());
            var option = new VrmLib.ExportArgs
            {
            };
            exporter10.Export(go, model, converter, option);
            return exporter10.Storage.ToBytes();
        }
    }
}
