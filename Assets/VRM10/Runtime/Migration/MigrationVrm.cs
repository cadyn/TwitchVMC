﻿using System;
using System.Collections.Generic;
using System.Linq;
using UniJSON;

namespace UniVRM10
{
    /// <summary>
    /// Convert vrm0 binary to vrm1 binary. Json processing
    /// </summary>
    public static class MigrationVrm
    {
        public static byte[] Migrate(byte[] src)
        {
            var glb = UniGLTF.Glb.Parse(src);
            var json = glb.Json.Bytes.ParseAsJson();
            return Migrate(json, glb.Binary.Bytes);
        }

        public static byte[] Migrate(JsonNode json, ArraySegment<byte> bin)
        {
            var gltf = UniGLTF.GltfDeserializer.Deserialize(json);

            // attach glb bin to buffer
            foreach (var buffer in gltf.buffers)
            {
                buffer.OpenStorage(new UniGLTF.SimpleStorage(bin));
            }

            // https://github.com/vrm-c/vrm-specification/issues/205
            RotateY180.Rotate(gltf);

            var vrm0 = json["extensions"]["VRM"];
            gltf.extensions = null;

            {
                // vrm
                var vrm1 = new UniGLTF.Extensions.VRMC_vrm.VRMC_vrm();

                // meta (required)
                vrm1.Meta = MigrationVrmMeta.Migrate(vrm0["meta"]);
                // humanoid (required)
                vrm1.Humanoid = MigrationVrmHumanoid.Migrate(vrm0["humanoid"]);

                // blendshape (optional)
                if (vrm0.TryGet("blendShapeMaster", out JsonNode vrm0BlendShape))
                {
                    vrm1.Expressions = new UniGLTF.Extensions.VRMC_vrm.Expressions
                    {
                        Preset = new UniGLTF.Extensions.VRMC_vrm.Preset(),
                        Custom = new Dictionary<string, UniGLTF.Extensions.VRMC_vrm.Expression>(),
                    };
                    foreach (var (preset, customName, expression) in MigrationVrmExpression.Migrate(gltf, vrm0BlendShape))
                    {
                        switch (preset)
                        {
                            case ExpressionPreset.happy: vrm1.Expressions.Preset.Happy = expression; break;
                            case ExpressionPreset.angry: vrm1.Expressions.Preset.Angry = expression; break;
                            case ExpressionPreset.sad: vrm1.Expressions.Preset.Sad = expression; break;
                            case ExpressionPreset.relaxed: vrm1.Expressions.Preset.Relaxed = expression; break;
                            case ExpressionPreset.surprised: vrm1.Expressions.Preset.Surprised = expression; break;
                            case ExpressionPreset.aa: vrm1.Expressions.Preset.Aa = expression; break;
                            case ExpressionPreset.ih: vrm1.Expressions.Preset.Ih = expression; break;
                            case ExpressionPreset.ou: vrm1.Expressions.Preset.Ou = expression; break;
                            case ExpressionPreset.ee: vrm1.Expressions.Preset.Ee = expression; break;
                            case ExpressionPreset.oh: vrm1.Expressions.Preset.Oh = expression; break;
                            case ExpressionPreset.blink: vrm1.Expressions.Preset.Blink = expression; break;
                            case ExpressionPreset.blinkLeft: vrm1.Expressions.Preset.BlinkLeft = expression; break;
                            case ExpressionPreset.blinkRight: vrm1.Expressions.Preset.BlinkRight = expression; break;
                            case ExpressionPreset.lookUp: vrm1.Expressions.Preset.LookUp = expression; break;
                            case ExpressionPreset.lookDown: vrm1.Expressions.Preset.LookDown = expression; break;
                            case ExpressionPreset.lookLeft: vrm1.Expressions.Preset.LookLeft = expression; break;
                            case ExpressionPreset.lookRight: vrm1.Expressions.Preset.LookRight = expression; break;
                            case ExpressionPreset.neutral: vrm1.Expressions.Custom[customName] = expression; break;
                            case ExpressionPreset.custom: vrm1.Expressions.Custom[customName] = expression; break;
                            default: throw new NotImplementedException();
                        }
                    }
                }

                // lookat & firstperson (optional)
                if (vrm0.TryGet("firstPerson", out JsonNode vrm0FirstPerson))
                {
                    (vrm1.LookAt, vrm1.FirstPerson) = MigrationVrmLookAtAndFirstPerson.Migrate(gltf, vrm0FirstPerson);
                }

                UniGLTF.Extensions.VRMC_vrm.GltfSerializer.SerializeTo(ref gltf.extensions, vrm1);
            }

            // springBone & collider (optional)
            if (vrm0.TryGet("secondaryAnimation", out JsonNode vrm0SpringBone))
            {
                var springBone = MigrationVrmSpringBone.Migrate(gltf, vrm0SpringBone);
                UniGLTF.Extensions.VRMC_springBone.GltfSerializer.SerializeTo(ref gltf.extensions, springBone);
            }

            // MToon                    
            {
                MigrationMToon.Migrate(gltf, json);
            }

            // Serialize whole glTF
            ArraySegment<byte> vrm1Json = default;
            {
                var f = new JsonFormatter();
                UniGLTF.GltfSerializer.Serialize(f, gltf);
                vrm1Json = f.GetStoreBytes();
            }
            // JSON 部分だけが改変されて、BIN はそのまま
            return UniGLTF.Glb.Create(vrm1Json, bin).ToBytes();
        }

        public static void Check(JsonNode vrm0, UniGLTF.Extensions.VRMC_vrm.VRMC_vrm vrm1)
        {
            MigrationVrmMeta.Check(vrm0["meta"], vrm1.Meta);
            MigrationVrmHumanoid.Check(vrm0["humanoid"], vrm1.Humanoid);
        }

        public static void Check(JsonNode vrm0, UniGLTF.Extensions.VRMC_springBone.VRMC_springBone vrm1, List<UniGLTF.glTFNode> nodes)
        {
            // Migration.CheckSpringBone(vrm0["secondaryAnimation"], vrm1.sp)
        }
    }
}
