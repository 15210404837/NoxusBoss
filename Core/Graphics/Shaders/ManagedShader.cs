using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Core.Graphics.Shaders
{
    public class ManagedShader
    {
        public readonly Ref<Effect> Shader;

        public const string TextureSizeParameterPrefix = "textureSize";

        public const string DefaultPassName = "AutoloadPass";

        public ManagedShader(Ref<Effect> shader) => Shader = shader;

        public bool TrySetParameter(string parameterName, object value)
        {
            // Shaders do not work on servers. If this method is called on one, terminate it immediately.
            if (Main.netMode == NetmodeID.Server)
                return false;

            EffectParameter parameter = Shader.Value.Parameters[parameterName];
            if (parameter is null)
                return false;

            // Unfortunately, there is no simple type upon which singles, ints, matrices, etc. can be converted in order to be sent to the GPU, and there is no
            // super easy solution for checking a parameter's expected type (FNA just messes with pointers under the hood and tosses back exceptions if that doesn't work).
            // Unless something neater arises, this conditional chain will do, I suppose.

            // Booleans.
            if (value is bool b)
            {
                parameter.SetValue(b);
                return true;
            }
            if (value is bool[] b2)
            {
                parameter.SetValue(b2);
                return true;
            }

            // Integers.
            if (value is int i)
            {
                parameter.SetValue(i);
                return true;
            }
            if (value is int[] i2)
            {
                parameter.SetValue(i2);
                return true;
            }

            // Floats.
            if (value is float f)
            {
                parameter.SetValue(f);
                return true;
            }
            if (value is float[] f2)
            {
                parameter.SetValue(f2);
                return true;
            }

            // Vector2s.
            if (value is Vector2 v2)
            {
                parameter.SetValue(v2);
                return true;
            }
            if (value is Vector2[] v22)
            {
                parameter.SetValue(v22);
                return true;
            }

            // Vector3s.
            if (value is Vector3 v3)
            {
                parameter.SetValue(v3);
                return true;
            }
            if (value is Vector3[] v32)
            {
                parameter.SetValue(v32);
                return true;
            }

            // Vector4s.
            if (value is Vector4 v4)
            {
                parameter.SetValue(v4);
                return true;
            }
            if (value is Vector4[] v42)
            {
                parameter.SetValue(v42);
                return true;
            }

            // Matrices.
            if (value is Matrix m)
            {
                parameter.SetValue(m);
                return true;
            }
            if (value is Matrix[] m2)
            {
                parameter.SetValue(m2);
                return true;
            }

            // Textures, for if those are explicitly designed as parameters.
            if (value is Texture2D t)
            {
                parameter.SetValue(t);
                return true;
            }

            // None of the condition cases were met, and something went wrong.
            return false;
        }

        public void SetTexture(Asset<Texture2D> textureAsset, int textureIndex, SamplerState samplerStateOverride = null)
        {
            // Shaders do not work on servers. If this method is called on one, terminate it immediately.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Collect the texture.
            Texture2D texture = textureAsset.Value;
            SetTexture(texture, textureIndex, samplerStateOverride);
        }

        public void SetTexture(Texture2D texture, int textureIndex, SamplerState samplerStateOverride = null)
        {
            // Shaders do not work on servers. If this method is called on one, terminate it immediately.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Try to send texture sizes as parameters. Such parameters are optional, and no penalty is incurred if a shader decides that it doesn't need that data.
            TrySetParameter($"TextureSizeParameterPrefix{textureIndex}", texture.Size());

            // Grab the graphics device and send the texture to it.
            var gd = Main.instance.GraphicsDevice;
            gd.Textures[textureIndex] = texture;
            if (samplerStateOverride is not null)
                gd.SamplerStates[textureIndex] = samplerStateOverride;
        }

        public void Apply(string passName = DefaultPassName)
        {
            // Shaders do not work on servers. If this method is called on one, terminate it immediately.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Try to send the global time as a parameter. It is optional, and no penalty is incurred if a shader decides that it doesn't need that data for some reason.
            TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);

            Shader.Value.CurrentTechnique.Passes[passName].Apply();
        }
    }
}
