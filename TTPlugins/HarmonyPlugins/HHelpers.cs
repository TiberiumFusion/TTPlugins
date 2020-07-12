using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace com.tiberiumfusion.ttplugins.HarmonyPlugins
{
    /// <summary>
    /// Contains various helper and convenience methods for HPlugins to use.
    /// </summary>
    public static class HHelpers
    {
        #region Top Level Helpers

        /// <summary>
        /// Gets the Type specified by its full name from the Terraria assembly.
        /// </summary>
        /// <param name="typeFullName">The full name of the type to retrieve. Case-sensitive!</param>
        /// <param name="result">Output for the found Type.</param>
        /// <returns>True if the type was found, false if it was not.</returns>
        public static bool TryGetTerrariaType(string typeFullName, out Type result)
        {
            if (String.IsNullOrEmpty(typeFullName))
                throw new Exception("Provided typeFullName was null or 0 chracters.");

            if (HPluginApplicator.TerrariaAssembly == null)
                throw new Exception("This method has been called outside of a patch application lifecycle and thus cannot proceed.");

            Type foundType = HPluginApplicator.TerrariaAssembly.GetType(typeFullName, false, false);
            if (foundType == null)
            {
                result = null;
                return false;
            }

            result = foundType;
            return true;
        }
        /// <summary>
        /// Gets the Type specified by its full name from the proprietary ReLogic assembly.
        /// </summary>
        /// <param name="typeFullName">The full name of the type to retrieve. Case-sensitive!</param>
        /// <param name="result">Output for the found Type.</param>
        /// <returns>True if the type was found, false if it was not.</returns>
        public static bool TryGetReLogicType(string typeFullName, out Type result)
        {
            if (String.IsNullOrEmpty(typeFullName))
                throw new Exception("Provided typeFullName was null or 0 chracters.");

            if (HPluginApplicator.ReLogicAssembly == null)
            {
                HPluginApplicator.FindLoadedReLogicAssembly();
                if (HPluginApplicator.ReLogicAssembly == null)
                    throw new Exception("The proprietary ReLogic assembly was not found in the loaded assemblies.");
            }

            Type foundType = HPluginApplicator.ReLogicAssembly.GetType(typeFullName, false, false);
            if (foundType == null)
            {
                result = null;
                return false;
            }

            result = foundType;
            return true;
        }
        /// <summary>
        /// Gets the Type specified by its full name from any of the loaded XNA assemblies.
        /// </summary>
        /// <param name="typeFullName">The full name of the type to retrieve. Case-sensitive!</param>
        /// <param name="result">Output for the found Type.</param>
        /// <returns>True if the type was found, false if it was not.</returns>
        public static bool TryGetXNAType(string typeFullName, out Type result)
        {
            if (String.IsNullOrEmpty(typeFullName))
                throw new Exception("Provided typeFullName was null or 0 chracters.");
            
            foreach (Assembly asm in HPluginApplicator.XNAAssemblies)
            {
                Type foundType = asm.GetType(typeFullName, false, false);
                if (foundType != null)
                {
                    result = foundType;
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Checks if a type is defined in the Terraria assembly.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True or false</returns>
        public static bool IsTypeInTerrariaAssembly(Type type)
        {
            if (HPluginApplicator.TerrariaAssembly == null)
                throw new Exception("This method has been called outside of a patch application lifecycle and thus cannot proceed.");

            return (type.Assembly == HPluginApplicator.TerrariaAssembly);
        }
        /// <summary>
        /// Checks if a type is defined in the proprietary ReLogic assembly.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True or false</returns>
        public static bool IsTypeInReLogicAssembly(Type type)
        {
            // Because the ReLogic asm is lazy loaded, we should retry finding it if we haven't found it yet
            if (HPluginApplicator.ReLogicAssembly == null)
            {
                HPluginApplicator.FindLoadedReLogicAssembly();
                if (HPluginApplicator.ReLogicAssembly == null)
                    throw new Exception("The proprietary ReLogic assembly was not found in the loaded assemblies.");
            }

            return (type.Assembly == HPluginApplicator.ReLogicAssembly);
        }
        /// <summary>
        /// Checks if a type is defined in any loaded XNA assembly.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True or false</returns>
        public static bool IsTypeInAnyXNAAssembly(Type type)
        {
            foreach (Assembly asm in HPluginApplicator.XNAAssemblies)
            {
                if (type.Assembly == asm)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a type is defined in an assembly that is whitelisted for Reflection operations. Terraria, ReLogic, and most of XNA are the only whitelisted assemblies.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True or false.</returns>
        public static bool VerifyTypeForSecureReflectionUse(Type type)
        {
            return (IsTypeInTerrariaAssembly(type) || IsTypeInReLogicAssembly(type) || IsTypeInAnyXNAAssembly(type));
        }

        /// <summary>
        /// Creates an instance of the specified using the first found constructor.
        /// This method only operates on types that are defined inside the Terraria, ReLogic, or XNA assemblies. If the type is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="type">The type to activate an instance of.</param>
        /// <param name="ctorParams">An object[] of parameters to pass to the constructor. Set to null for no parameters.</param>
        /// <returns>The activated object.</returns>
        public static object ActivateInstanceUsingFirstConstructor(Type type, object[] ctorParams = null)
        {
            if (!VerifyTypeForSecureReflectionUse(type))// Only allow Reflection upon whitelisted assemblies for security purposes
                throw new Exception("Type \"" + type.FullName + "\" is not defined in the Terraria, ReLogic, or XNA assemblies. Activating instances of types outside of these assemblies with Reflection is prohibited.");

            ConstructorInfo ctor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
            if (ctor == null)
                throw new Exception("Type \"" + type.FullName + "\" does not have any constructors");
            
            return ctor.Invoke(ctorParams);
        }

        /// <summary>
        /// Retrieves the value of a field using Reflection. This method may incur considerable performance penalties.
        /// This method only operates on fields that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the field is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="fieldName">The name of the field which will be read. The sourceObject's type will be used to find the field.</param>
        /// <param name="sourceObject">The object that contains the field to be read.</param>
        /// <returns>The sourceObject's value of the field.</returns>
        public static object GetFieldValueWithReflection(string fieldName, object sourceObject)
        {
            Type objectType = sourceObject.GetType();
            return GetFieldValueWithReflection(objectType, fieldName, sourceObject);
        }
        /// <summary>
        /// Retrieves the value of a field using Reflection. This method may incur considerable performance penalties.
        /// This method only operates on fields that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the field is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="type">The Type that contains the specified field to read.</param>
        /// <param name="fieldName">The name of the field which will be read.</param>
        /// <param name="sourceObject">The object that contains the field to be read.</param>
        /// <returns>The sourceObject's value of the field.</returns>
        public static object GetFieldValueWithReflection(Type type, string fieldName, object sourceObject)
        {
            FieldInfo field = type.GetRuntimeFields().Where(x => x.Name == fieldName).FirstOrDefault();
            if (field == null)
                throw new Exception("Invalid type & fieldName combination. The specified type \"" + type.FullName + "\" does not contain a field named \"" + fieldName + "\".");

            return GetFieldValueWithReflection(field, sourceObject);
        }
        /// <summary>
        /// Retrieves the value of a field using Reflection. This method may incur considerable performance penalties.
        /// This method only operates on fields that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the field is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="field">The FieldInfo associated with the field to be read.</param>
        /// <param name="sourceObject">The object that contains the field to be read.</param>
        /// <returns>The sourceObject's value of the field.</returns>
        public static object GetFieldValueWithReflection(FieldInfo field, object sourceObject)
        {
            Type sourceObjectType = sourceObject.GetType();
            if (!VerifyTypeForSecureReflectionUse(sourceObjectType)) // Only allow Reflection upon whitelisted assemblies for security purposes
                throw new Exception("The type of sourceObject (" + sourceObjectType.FullName + ") is not defined in the Terraria, ReLogic, or XNA assemblies. Operating on types outside of these assemblies with Reflection is prohibited.");

            return field.GetValue(sourceObject);
        }


        /// <summary>
        /// Writes the value of a field using Reflection. This method may incur considerable performance penalties. 
        /// This method only operates on fields that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the field is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="fieldName">The name of the field which will be written to. The sourceObject's type will be used to find the field.</param>
        /// <param name="sourceObject">The object that contains the field to write to.</param>
        /// <param name="newFieldValue">The new value which will be assigned to the field.</param>
        public static void SetFieldValueWithReflection(string fieldName, object sourceObject, object newFieldValue)
        {
            Type objectType = sourceObject.GetType();
            SetFieldValueWithReflection(objectType, fieldName, sourceObject, newFieldValue);
        }
        /// <summary>
        /// Writes the value of a field using Reflection. This method may incur considerable performance penalties. 
        /// This method only operates on fields that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the field is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="type">The Type that contains the specified field to write to.</param>
        /// <param name="fieldName">The name of the field which will be written to.</param>
        /// <param name="sourceObject">The object that contains the field to write to.</param>
        /// <param name="newFieldValue">The new value which will be assigned to the field.</param>
        public static void SetFieldValueWithReflection(Type type, string fieldName, object sourceObject, object newFieldValue)
        {
            FieldInfo field = type.GetRuntimeFields().Where(x => x.Name == fieldName).FirstOrDefault();
            if (field == null)
                throw new Exception("Invalid type & fieldName combination. The specified type \"" + type.FullName + "\" does not contain a field named \"" + fieldName + "\".");

            SetFieldValueWithReflection(field, sourceObject, newFieldValue);
        }
        /// <summary>
        /// Writes the value of a field using Reflection. This method may incur considerable performance penalties. 
        /// This method only operates on fields that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the field is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="field">The FieldInfo associated with the field to write to.</param>
        /// <param name="sourceObject">The object that contains the field to write to.</param>
        /// <param name="newFieldValue">The new value which will be assigned to the field.</param>
        public static void SetFieldValueWithReflection(FieldInfo field, object sourceObject, object newFieldValue)
        {
            Type sourceObjectType = sourceObject.GetType();
            if (!VerifyTypeForSecureReflectionUse(sourceObjectType)) // Only allow Reflection upon whitelisted assemblies for security purposes
                throw new Exception("The type of sourceObject (" + sourceObjectType.FullName + ") is not defined in the Terraria, ReLogic, or XNA assemblies. Operating on types outside of these assemblies with Reflection is prohibited.");

            field.SetValue(sourceObject, newFieldValue);
        }

        
        /// <summary>
        /// Gets the value of a property using Reflection. This method may incur considerable performance penalties.
        /// This method only operates on properties that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the property is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="propertyName">The name of the property which will be read. The sourceObject's type will be used to find the property.</param>
        /// <param name="sourceObject">The object that contains the property to be read.</param>
        /// <returns>The sourceObject's value of the property.</returns>
        public static object GetPropertyValueWithReflection(string propertyName, object sourceObject)
        {
            Type objectType = sourceObject.GetType();
            return GetPropertyValueWithReflection(objectType, propertyName, sourceObject);
        }
        /// <summary>
        /// Gets the value of a property using Reflection. This method may incur considerable performance penalties.
        /// This method only operates on properties that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the property is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="type">The Type that contains the specified property to read.</param>
        /// <param name="propertyName">The name of the property which will be read. The sourceObject's type will be used to find the property.</param>
        /// <param name="sourceObject">The object that contains the property to be read.</param>
        /// <returns>The sourceObject's value of the property.</returns>
        public static object GetPropertyValueWithReflection(Type type, string propertyName, object sourceObject)
        {
            PropertyInfo property = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.Name == propertyName).FirstOrDefault();
            if (property == null)
                throw new Exception("Invalid type & propertyName combination. The specified type \"" + type.FullName + "\" does not contain a property named \"" + propertyName + "\".");

            return GetPropertyValueWithReflection(property, sourceObject);
        }
        /// <summary>
        /// Gets the value of a property using Reflection. This method may incur considerable performance penalties.
        /// This method only operates on properties that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the property is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="property">The PropertyInfo associated with the property to be read.</param>
        /// <param name="sourceObject">The object that contains the property to be read.</param>
        /// <returns>The sourceObject's value of the property.</returns>
        public static object GetPropertyValueWithReflection(PropertyInfo property, object sourceObject)
        {
            Type sourceObjectType = sourceObject.GetType();
            if (!VerifyTypeForSecureReflectionUse(sourceObjectType)) // Only allow Reflection upon whitelisted assemblies for security purposes
                throw new Exception("The type of sourceObject (" + sourceObjectType.FullName + ") is not defined in the Terraria, ReLogic, or XNA assemblies. Operating on types outside of these assemblies with Reflection is prohibited.");

            return property.GetValue(sourceObject);
        }
        
        /// <summary>
        /// Sets the value of a property using Reflection. This method may incur considerable performance penalties.
        /// This method only operates on properties that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the property is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="propertyName">The name of the property which will be written to. The sourceObject's type will be used to find the property.</param>
        /// <param name="sourceObject">The object that contains the property to write to.</param>
        /// <param name="newFieldValue">The new value which will be assigned to the property.</param>
        public static void SetPropertyValueWithReflection(string propertyName, object sourceObject, object newFieldValue)
        {
            Type objectType = sourceObject.GetType();
            SetPropertyValueWithReflection(objectType, propertyName, sourceObject, newFieldValue);
        }
        /// <summary>
        /// Sets the value of a property using Reflection. This method may incur considerable performance penalties.
        /// This method only operates on properties that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the property is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="type">The Type that contains the specified property to set.</param>
        /// <param name="propertyName">The name of the property which will be set.</param>
        /// <param name="sourceObject">The object that contains the property to set.</param>
        /// <param name="newFieldValue">The new value which will be assigned to the property.</param>
        public static void SetPropertyValueWithReflection(Type type, string propertyName, object sourceObject, object newFieldValue)
        {
            PropertyInfo property = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.Name == propertyName).FirstOrDefault();
            if (property == null)
                throw new Exception("Invalid type & propertyName combination. The specified type \"" + type.FullName + "\" does not contain a property named \"" + propertyName + "\".");

            SetPropertyValueWithReflection(property, sourceObject, newFieldValue);
        }
        /// <summary>
        /// Sets the value of a property using Reflection. This method may incur considerable performance penalties.
        /// This method only operates on properties that are defined by types inside the Terraria, ReLogic, or XNA assemblies. If the type that owns the property is not defined in the Terraria, ReLogic, or XNA assemblies, an exception is thrown.
        /// </summary>
        /// <param name="property">The PropertyInfo associated with the property to set.</param>
        /// <param name="sourceObject">The object that contains the property to set.</param>
        /// <param name="newFieldValue">The new value which will be assigned to the property.</param>
        public static void SetPropertyValueWithReflection(PropertyInfo property, object sourceObject, object newFieldValue)
        {
            Type sourceObjectType = sourceObject.GetType();
            if (!VerifyTypeForSecureReflectionUse(sourceObjectType)) // Only allow Reflection upon whitelisted assemblies for security purposes
                throw new Exception("The type of sourceObject (" + sourceObjectType.FullName + ") is not defined in the Terraria, ReLogic, or XNA assemblies. Operating on types outside of these assemblies with Reflection is prohibited.");

            property.SetValue(sourceObject, newFieldValue);
        }
        

        /// <summary>
        /// Returns an array of all Assemblies which are loaded in the current AppDomain.
        /// This is simply a wrapper for AppDomain.CurrentDomain.GetAssemblies().
        /// </summary>
        /// <returns>An array containing all Assemblies which are loaded in the current AppDomain.</returns>
        public static Assembly[] GetAssembliesInCurrentAppDomain()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }


        /// <summary>
        /// Gets the value of a field (using Reflection) as a dynamic object. Useful in working around "type MyType is not the same type as MyType" issues that arise from Assembly location mismatches.
        /// </summary>
        /// <param name="field">The FieldInfo to retrieve the value of.</param>
        /// <param name="instance">The object to retrieve the field value of.</param>
        /// <returns>The value of the field, as a dynamic object.</returns>
        public static dynamic GetFieldValueByReflectionAsDynamic(FieldInfo field, object instance = null)
        {
            dynamic fieldValue = Convert.ChangeType(field.GetValue(instance), field.FieldType);
            return fieldValue;
        }

        #endregion


        #region Asset Handling Helpers

        /// <summary>
        /// Specifically contains helper methods for use in creating and managing XNA assets.
        /// </summary>
        public static class AssetHandling
        {
            /// <summary>
            /// Creates a new Texture2D from a byte[] which constitutes a JPG, PNG, or GIF image (i.e. NOT an XNB file).
            /// This helper is particularly useful when the plugin Security Level is set to Level 3 or higher (which disallows use of System.IO).
            /// </summary>
            /// <param name="imageBytes">The JPG, PNG, or GIF image, as a byte array.</param>
            /// <param name="graphicsDevice">The active GraphicsDevice, which is required to create a Texture2D.</param>
            /// <param name="doPremultiply">Will premultiply the channels of the final texture. Set this to true if your source image has non-premultiplied channels and partial transparency (e.g. fading gradients).</param>
            /// <param name="gammaCorrection">If non-null, gamma correction will be applied to the final texture. All pixels will be raised to 1/gammaCorrection power, where XYZW in the Vector4 maps to RGBA in the texture.</param>
            /// <returns>The created Texture2D.</returns>
            public static Texture2D CreateTexture2DFromImageBytes(byte[] imageBytes, GraphicsDevice graphicsDevice, bool doPremultiply = false, Vector4? gammaCorrection = null)
            {
                Texture2D result = null;
                using (MemoryStream memStream = new MemoryStream(imageBytes))
                {
                    result = Texture2D.FromStream(graphicsDevice, memStream);
                }

                if (gammaCorrection != null)
                    GammaCorrectTexture(result, (Vector4)gammaCorrection);

                if (doPremultiply)
                    PremultiplyTexture(result);

                return result;
            }
            /// <summary>
            /// Creates a new Texture2D from a byte[] which constitutes a JPG, PNG, or GIF image (i.e. NOT an XNB file).
            /// This helper is particularly useful when the plugin Security Level is set to Level 3 or higher (which disallows use of System.IO).
            /// </summary>
            /// <param name="imageBytes">The JPG, PNG, or GIF image, as a byte array.</param>
            /// <param name="graphicsDevice">The active GraphicsDevice, which is required to create a Texture2D.</param>
            /// <param name="width">The desired image width. Mirror of Texture2D.FromStream()'s width parameter.</param>
            /// <param name="height">The desired image height. Mirror of Texture2D.FromStream()'s height parameter.</param>
            /// <param name="zoom">Controls aspect ratio. False for constant aspect ratio, true for otherwise. Mirror of Texture2D.FromStream()'s zoom parameter.</param>
            /// <param name="doPremultiply">Will premultiply the channels of the final texture. Set this to true if your source image has non-premultiplied channels and partial transparency (e.g. fading gradients).</param>
            /// <param name="gammaCorrection">If non-null, gamma correction will be applied to the final texture. All pixels will be raised to 1/gammaCorrection power, where XYZW in the Vector4 maps to RGBA in the texture.</param>
            /// <returns>The created Texture2D.</returns>
            public static Texture2D CreateTexture2DFromImageBytes(byte[] imageBytes, GraphicsDevice graphicsDevice, int width, int height, bool zoom, bool doPremultiply = false, Vector4? gammaCorrection = null)
            {
                Texture2D result = null;
                using (MemoryStream memStream = new MemoryStream(imageBytes))
                {
                    result = Texture2D.FromStream(graphicsDevice, memStream, width, height, zoom);
                }

                if (gammaCorrection != null)
                    GammaCorrectTexture(result, (Vector4)gammaCorrection);

                if (doPremultiply)
                    PremultiplyTexture(result);

                return result;
            }

            /// <summary>
            /// Premultiplies the color channels of a texture. Should only be used on non-premultiplied textures.
            /// </summary>
            /// <param name="texture">The texture to premultiply.</param>
            public static void PremultiplyTexture(Texture2D texture)
            {
                Color[] texBuffer = new Color[texture.Width * texture.Height];
                texture.GetData(texBuffer);
                for (int i = 0; i < texBuffer.Length; i++)
                    texBuffer[i] = Color.FromNonPremultiplied(texBuffer[i].R, texBuffer[i].G, texBuffer[i].B, texBuffer[i].A);
                texture.SetData(texBuffer);
            }

            /// <summary>
            /// Performs gamma correction on a texture. All pixels will be raised to 1/gammaCorrection power, where XYZW in the Vector4 maps to RGBA in the texture.
            /// </summary>
            /// <param name="texture">The texture to process.</param>
            /// <param name="gammaPower">The texture's current gamma. Most programs save their JPGs and PNGs in a 2.0 or 2.2 gamma space.</param>
            public static void GammaCorrectTexture(Texture2D texture, Vector4 gammaPower)
            {
                double powerR = 1.0 / (double)gammaPower.X;
                double powerG = 1.0 / (double)gammaPower.Y;
                double powerB = 1.0 / (double)gammaPower.Z;
                double powerA = 1.0 / (double)gammaPower.W;
                Color[] texBuffer = new Color[texture.Width * texture.Height];
                texture.GetData(texBuffer);
                for (int i = 0; i < texBuffer.Length; i++)
                {
                    Vector4 col = texBuffer[i].ToVector4();
                    col.X = (float)Math.Pow(col.X, powerR);
                    col.Y = (float)Math.Pow(col.Y, powerG);
                    col.Z = (float)Math.Pow(col.Z, powerB);
                    col.W = (float)Math.Pow(col.W, powerA);
                    texBuffer[i] = new Color(col);
                }
                texture.SetData(texBuffer);
            }
        }

        #endregion


        #region String Drawing Helpers

        /// <summary>
        /// Specifically contains helper methods for use in managing spritefonts and drawing strings.
        /// </summary>
        public static class StringDrawing
        {
            /// <summary>
            /// Convenience method for drawing some text on the screen with a ReLogic.Graphics.DynamicSpriteFont.
            /// </summary>
            /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
            /// <param name="relogicDynamicSpriteFont">The ReLogic.Graphics.DynamicSpriteFont to use.</param>
            /// <param name="text">The text to drawn.</param>
            /// <param name="position">The viewportspace position to draw the text at.</param>
            /// <param name="baseColor">The color of the drawn text.</param>
            /// <param name="rotation">The rotation of the drawn text (in radians).</param>
            /// <param name="origin">The origin position of the drawn text.</param>
            /// <param name="baseScale">The scale of the drawn text.</param>
            /// <param name="maxWidth">The maximum width per line of the drawn text. Set to -1f for no max width.</param>
            public static void DrawString(SpriteBatch spriteBatch, ReLogic.Graphics.DynamicSpriteFont relogicDynamicSpriteFont, string text, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, float maxWidth = -1f)
            {
                Terraria.UI.Chat.TextSnippet message = new Terraria.UI.Chat.TextSnippet(text);
                Terraria.UI.Chat.TextSnippet[] snippets = new Terraria.UI.Chat.TextSnippet[] { message };
                Terraria.UI.Chat.ChatManager.ConvertNormalSnippets(snippets);

                int dummy = 0;
                FixedDrawColorCodedString(Terraria.Main.spriteBatch, relogicDynamicSpriteFont, snippets,
                                                                        position, baseColor, rotation,
                                                                        origin, baseScale, out dummy, maxWidth, true);
            }
            /// <summary>
            /// Convenience method for drawing some shadowed text on the screen with a ReLogic.Graphics.DynamicSpriteFont.
            /// </summary>
            /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
            /// <param name="relogicDynamicSpriteFont">The ReLogic.Graphics.DynamicSpriteFont to use.</param>
            /// <param name="text">The text to drawn.</param>
            /// <param name="position">The viewportspace position to draw the text at.</param>
            /// <param name="baseColor">The color of the drawn text.</param>
            /// <param name="rotation">The rotation of the drawn text (in radians).</param>
            /// <param name="origin">The origin position of the drawn text.</param>
            /// <param name="baseScale">The scale of the drawn text.</param>
            /// <param name="maxWidth">The maximum width per line of the drawn text. Set to -1f for no max width.</param>
            /// <param name="spread">The spread of the text's shadow.</param>
            public static void DrawStringWithShadow(SpriteBatch spriteBatch, ReLogic.Graphics.DynamicSpriteFont relogicDynamicSpriteFont, string text, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, float maxWidth = -1f, float spread = 2f)
            {
                Terraria.UI.Chat.TextSnippet message = new Terraria.UI.Chat.TextSnippet(text);
                Terraria.UI.Chat.TextSnippet[] snippets = new Terraria.UI.Chat.TextSnippet[] { message };
                Terraria.UI.Chat.ChatManager.ConvertNormalSnippets(snippets);

                FixedDrawColorCodedStringShadow(Terraria.Main.spriteBatch, relogicDynamicSpriteFont, snippets,
                                                                        position, new Color(0, 0, 0, baseColor.A), rotation,
                                                                        origin, baseScale, maxWidth, spread);
                int dummy = 0;
                FixedDrawColorCodedString(Terraria.Main.spriteBatch, relogicDynamicSpriteFont, snippets,
                                                                        position, baseColor, rotation,
                                                                        origin, baseScale, out dummy, maxWidth, true);
            }

            /// <summary>
            /// Creates a type-fixed copy of a ReLogic.Graphics.DynamicSpriteFont specified by field name in the Terraria.GameContent.FontAssets type.
            /// This works around the "type of ReLogic.Graphics.DynamicSpriteFont is not the same as type of ReLogic.Graphics.DynamicSpriteFont" issue that occurs when the propriety ReLogic assembly was loaded without context.
            /// This is a very expensive operation and should be used sparingly. Call this once and store the result to use later.
            /// </summary>
            /// <param name="fontFieldName">The field name of the ReLogic.Content.Asset&lt;ReLogic.Graphics.DynamicSpriteFont&gt; to copy and type-fix.</param>
            /// <param name="result">The ReLogic.Graphics.DynamicSpriteFont to output the result to.</param>
            /// <returns>True if the operation succeeded, false if otherwise.</returns>
            public static bool TryCreateTypeFixedDynamicSpriteFontFromExisting(string fontFieldName, out ReLogic.Graphics.DynamicSpriteFont result)
            {
                try
                {
                    dynamic SFontFieldValue = HHelpers.GetFieldValueByReflectionAsDynamic(typeof(Terraria.GameContent.FontAssets).GetField(fontFieldName));
                    dynamic RLSFont = SFontFieldValue.Value;
                    
                    Type TypeDynamicSpriteFontSource = RLSFont.GetType();
                    FieldInfo _characterSpacingSource = TypeDynamicSpriteFontSource.GetField("_characterSpacing", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo _lineSpacingSource = TypeDynamicSpriteFontSource.GetField("_lineSpacing", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo DefaultCharacterSource = TypeDynamicSpriteFontSource.GetField("DefaultCharacter");
                    FieldInfo _spriteCharactersSource = TypeDynamicSpriteFontSource.GetField("_spriteCharacters", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo _defaultCharacterDataSource = TypeDynamicSpriteFontSource.GetField("_defaultCharacterData", BindingFlags.NonPublic | BindingFlags.Instance);
                    Type TypeSpriteCharacterDataSource = _defaultCharacterDataSource.FieldType;

                    ReLogic.Graphics.DynamicSpriteFont sfont = new ReLogic.Graphics.DynamicSpriteFont(0f, 0, '*');
                    Type TypeDynamicSpriteFontTarget = sfont.GetType();
                    FieldInfo _characterSpacingTarget = TypeDynamicSpriteFontTarget.GetField("_characterSpacing", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo _lineSpacingTarget = TypeDynamicSpriteFontTarget.GetField("_lineSpacing", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo DefaultCharacterTarget = TypeDynamicSpriteFontTarget.GetField("DefaultCharacter");
                    FieldInfo _spriteCharactersTarget = TypeDynamicSpriteFontTarget.GetField("_spriteCharacters", BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo _defaultCharacterDataTarget = TypeDynamicSpriteFontTarget.GetField("_defaultCharacterData", BindingFlags.NonPublic | BindingFlags.Instance);
                    Type TypeSpriteCharacterDataTarget = _defaultCharacterDataTarget.FieldType;

                    _characterSpacingTarget.SetValue(sfont, (float)_characterSpacingSource.GetValue(RLSFont));
                    _lineSpacingTarget.SetValue(sfont, (int)_lineSpacingSource.GetValue(RLSFont));
                    DefaultCharacterTarget.SetValue(sfont, (char)DefaultCharacterSource.GetValue(RLSFont));
                    object _spriteCharactersSource_Value = _spriteCharactersSource.GetValue(RLSFont);
                    IDictionary _spriteCharactersSource_ValueAsIDictionary = (IDictionary)_spriteCharactersSource_Value;
                    object _spriteCharactersTarget_Value = _spriteCharactersTarget.GetValue(sfont);
                    IDictionary _spriteCharactersTarget_ValueAsIDictionary = (IDictionary)_spriteCharactersTarget_Value;

                    foreach (char key in _spriteCharactersSource_ValueAsIDictionary.Keys)
                    {
                        object spriteCharacterDataNew = _spriteCharactersSource_ValueAsIDictionary[key];
                        _spriteCharactersTarget_ValueAsIDictionary[key] = TypeConvertFix_SpriteCharacterData(spriteCharacterDataNew, TypeSpriteCharacterDataTarget);
                    }
                    _defaultCharacterDataTarget.SetValue(sfont, TypeConvertFix_SpriteCharacterData(_defaultCharacterDataSource.GetValue(RLSFont), TypeSpriteCharacterDataTarget));

                    result = sfont;
                    return true;
                }
                catch (Exception e)
                {
                    result = null;
                    return false;
                }
            }
            private static object TypeConvertFix_SpriteCharacterData(dynamic source, Type typeOfTarget)
            {
                Type sourceType = source.GetType();
                FieldInfo fieldTexture = sourceType.GetFields().Where(x => x.Name == "Texture").FirstOrDefault();
                FieldInfo fieldGlyph = sourceType.GetFields().Where(x => x.Name == "Glyph").FirstOrDefault();
                FieldInfo fieldPadding = sourceType.GetFields().Where(x => x.Name == "Padding").FirstOrDefault();
                FieldInfo fieldKerning = sourceType.GetFields().Where(x => x.Name == "Kerning").FirstOrDefault();
                object[] rippedValues = new object[]
                {
                    (Texture2D)fieldTexture.GetValue(source),
                    (Rectangle)fieldGlyph.GetValue(source),
                    (Rectangle)fieldPadding.GetValue(source),
                    (Vector3)fieldKerning.GetValue(source),
                };
                return Activator.CreateInstance(typeOfTarget, args: rippedValues);
            }
            
            /// <summary>
            /// Improved version of Terraria.UI.Chat.ChatManager.DrawColorCodedString without the stupidity. Doesn't split strings on whitespace and correctly handles rotation.
            /// You probably shouldn't use this method directly. Use DrawString() or DrawStringWithShadow() instead.
            /// </summary>
            /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
            /// <param name="relogicDynamicSpriteFont">The ReLogic.Graphics.DynamicSpriteFont to use. Must be type-fixed (see TryCreateTypeFixedDynamicSpriteFontFromExisting()).</param>
            /// <param name="snippets">An array of Terraria.UI.Chat.TextSnippets to draw.</param>
            /// <param name="position">The viewportspace position to draw the text at.</param>
            /// <param name="baseColor">The color of the drawn text.</param>
            /// <param name="rotation">The rotation angle of the drawn text (in radians).</param>
            /// <param name="origin">The origin of the drawn text position.</param>
            /// <param name="baseScale">The scale of the drawn text.</param>
            /// <param name="hoveredSnippet">Related to internal Terraria functions.</param>
            /// <param name="maxWidth">Maximum width per line of text.</param>
            /// <param name="ignoreColors">Related to internal Terraria functions.</param>
            /// <returns>The size of the drawn text.</returns>
            internal static Vector2 FixedDrawColorCodedString(SpriteBatch spriteBatch, dynamic relogicDynamicSpriteFont, Terraria.UI.Chat.TextSnippet[] snippets, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, out int hoveredSnippet, float maxWidth, bool ignoreColors = false)
            {
                int num = -1;
                Vector2 vec = new Vector2(Terraria.Main.mouseX, Terraria.Main.mouseY);
                Vector2 vector = position;
                Vector2 result = vector;
                float x = relogicDynamicSpriteFont.MeasureString(" ").X;
                Color color = baseColor;
                float num2 = 1f;
                float num3 = 0f;
                for (int i = 0; i < snippets.Length; i++)
                {
                    Terraria.UI.Chat.TextSnippet textSnippet = snippets[i];
                    textSnippet.Update();
                    if (!ignoreColors)
                    {
                        color = textSnippet.GetVisibleColor();
                    }
                    num2 = textSnippet.Scale;
                    Vector2 size;
                    if (textSnippet.UniqueDraw(false, out size, spriteBatch, vector, color, num2))
                    {
                        if (FixedDrawColorCodedString_Between(vec, vector, vector + size))
                        {
                            num = i;
                        }
                        vector.X += size.X * baseScale.X * num2;
                        result.X = Math.Max(result.X, vector.X);
                        continue;
                    }
                    string[] array = textSnippet.Text.Split('\n');
                    array = Regex.Split(textSnippet.Text, "(\n)");
                    bool flag = true;
                    foreach (string text in array)
                    {
                        //string[] array2 = Regex.Split(text, "( )");
                        //array2 = text.Split(' ');
                        string[] array2 = new string[] { text };
                        if (text == "\n")
                        {
                            vector.Y += (float)relogicDynamicSpriteFont.LineSpacing * num3 * baseScale.Y;
                            vector.X = position.X;
                            result.Y = Math.Max(result.Y, vector.Y);
                            num3 = 0f;
                            flag = false;
                            continue;
                        }
                        for (int k = 0; k < array2.Length; k++)
                        {
                            if (k != 0)
                            {
                                vector.X += x * baseScale.X * num2;
                            }
                            if (maxWidth > 0f)
                            {
                                float num4 = relogicDynamicSpriteFont.MeasureString(array2[k]).X * baseScale.X * num2;
                                if (vector.X - position.X + num4 > maxWidth)
                                {
                                    vector.X = position.X;
                                    vector.Y += (float)relogicDynamicSpriteFont.LineSpacing * num3 * baseScale.Y;
                                    result.Y = Math.Max(result.Y, vector.Y);
                                    num3 = 0f;
                                }
                            }
                            if (num3 < num2)
                            {
                                num3 = num2;
                            }
                            RelogicDyanmicSpriteFront_DrawString(spriteBatch, relogicDynamicSpriteFont, array2[k], vector, color, rotation, origin, baseScale * textSnippet.Scale * num2, SpriteEffects.None, 0f);
                            Vector2 value = relogicDynamicSpriteFont.MeasureString(array2[k]);
                            if (FixedDrawColorCodedString_Between(vec, vector, vector + value))
                            {
                                num = i;
                            }
                            vector.X += value.X * baseScale.X * num2;
                            result.X = Math.Max(result.X, vector.X);
                        }
                        if (array.Length > 1 && flag)
                        {
                            vector.Y += (float)relogicDynamicSpriteFont.LineSpacing * num3 * baseScale.Y;
                            vector.X = position.X;
                            result.Y = Math.Max(result.Y, vector.Y);
                            num3 = 0f;
                        }
                        flag = true;
                    }
                }
                hoveredSnippet = num;
                return result;
            }
            internal static void FixedDrawColorCodedStringShadow(SpriteBatch spriteBatch, dynamic font, Terraria.UI.Chat.TextSnippet[] snippets, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, float maxWidth = -1f, float spread = 2f)
            {
                for (int i = 0; i < Terraria.UI.Chat.ChatManager.ShadowDirections.Length; i++)
                {
                    int hoveredSnippet;
                    FixedDrawColorCodedString(spriteBatch, font, snippets, position + Terraria.UI.Chat.ChatManager.ShadowDirections[i] * spread, baseColor, rotation, origin, baseScale, out hoveredSnippet, maxWidth, true);
                }
            }
            private static bool FixedDrawColorCodedString_Between(Vector2 vec, Vector2 minimum, Vector2 maximum)
            {
                if (vec.X >= minimum.X && vec.X <= maximum.X && vec.Y >= minimum.Y)
                {
                    return vec.Y <= maximum.Y;
                }
                return false;
            }
            private static Dictionary<dynamic, MethodInfo> CachedInternalDrawMethodInfos = new Dictionary<dynamic, MethodInfo>();
            private static void RelogicDyanmicSpriteFront_DrawString(SpriteBatch spriteBatch, dynamic spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
            {
                Type TypeSpriteFont = spriteFont.GetType();

                // We can't use our own copypasta for InternalDraw because it uses private methods
                // So we use reflection to invoke it instead, which is ugly and slow, but literally nothing else will work
                MethodInfo methodInternalDraw = null;
                if (!CachedInternalDrawMethodInfos.TryGetValue(spriteFont, out methodInternalDraw))
                {
                    methodInternalDraw = TypeSpriteFont.GetRuntimeMethods().Where(x => x.Name == "InternalDraw").FirstOrDefault();
                    if (methodInternalDraw == null)
                        throw new Exception("The provided spriteFont does not contain any method named InternalDraw.");
                    CachedInternalDrawMethodInfos[spriteFont] = methodInternalDraw;
                }
                methodInternalDraw.Invoke(spriteFont, new object[] { text, spriteBatch, position, color, rotation, origin, scale, effects, layerDepth });
            }
        }

        #endregion

        #region Input Reading Helpers
        
        /// <summary>
        /// Specifically contains helper methods for use in reading human input.
        /// </summary>
        public static class InputReading
        {
            /// <summary>
            /// Checks whether the specified key is currently held or not.
            /// </summary>
            /// <param name="k">The key to check.</param>
            /// <returns>True or false</returns>
            public static bool IsKeyDown(Keys k)
            {
                if (k == Keys.None) return false;
                return Terraria.Main.keyState.IsKeyDown(k);
            }

            /// <summary>
            /// Checks whether the specified key was down on this update cycle and up on the last update cycle.
            /// </summary>
            /// <param name="k">The key to check.</param>
            /// <returns>True or false</returns>
            public static bool IsKeyPressed(Keys k)
            {
                if (k == Keys.None) return false;
                return (Terraria.Main.keyState.IsKeyDown(k) && !Terraria.Main.oldKeyState.IsKeyDown(k));
            }
            
            /// <summary>
            /// Checks whether the base key was down on this tick and up on the last tick AND that the modifier key is currently held.
            /// If the modifier key is Keys.None, the modifier key down check will be skipped.
            /// </summary>
            /// <param name="baseKey">The base key to check.</param>
            /// <param name="modifierKey">The modifier key to check.</param>
            /// <returns>True or false</returns>
            public static bool IsKeyComboPressed(Keys baseKey, Keys modifierKey)
            {
                if (baseKey == Keys.None) return false;
                if (Terraria.Main.keyState.IsKeyDown(baseKey) && !Terraria.Main.oldKeyState.IsKeyDown(baseKey))
                {
                    if (modifierKey == Keys.None)
                        return true;
                    else
                        return (Terraria.Main.keyState.IsKeyDown(modifierKey));
                }
                else
                    return false;
            }

            /// <summary>
            /// Checks whether the base key AND the modifier key are both currently held.
            /// If the modifier key is Keys.None, the modifier key down check will be skipped.
            /// </summary>
            /// <param name="baseKey">The base key to check.</param>
            /// <param name="modifierKey">The modifier key to check.</param>
            /// <returns>True or false</returns>
            public static bool IsKeyComboDown(Keys baseKey, Keys modifierKey)
            {
                if (baseKey == Keys.None) return false;

                if (modifierKey == Keys.None)
                    return (Terraria.Main.keyState.IsKeyDown(baseKey));
                else
                    return (Terraria.Main.keyState.IsKeyDown(baseKey) && Terraria.Main.keyState.IsKeyDown(modifierKey));
            }
        }

        #endregion

        #region UI State Helpers

        /// <summary>
        /// Specifically contains helper methods for use in detecting properties of the current UI state.
        /// </summary>
        public static class UIState
        {
            /// <summary>
            /// Returns true if the chat box has keyboard focus.
            /// </summary>
            /// <returns>True or false</returns>
            public static bool IsLocalPlayerTypingInChat()
            {
                //return Terraria.GameInput.PlayerInput.WritingText; // Doesn't work
                return Terraria.Main.drawingPlayerChat; // Works
            }

            /// <summary>
            /// Returns true if the local player is renaming a chest.
            /// </summary>
            /// <returns>True or false</returns>
            public static bool IsLocalPlayerRenamingChest()
            {
                return Terraria.Main.editChest;
            }

            /// <summary>
            /// Returns true if the local player is editing a sign.
            /// </summary>
            /// <returns>True or false.</returns>
            public static bool IsLocalPlayerEditingASign()
            {
                return Terraria.Main.editSign;
            }

            /// <summary>
            /// Return true if the local player is entering text in a search box of some sort.
            /// </summary>
            /// <returns>True or false</returns>
            public static bool IsLocalPlayerTypingInASearchBox()
            {
                return Terraria.Main.CurrentInputTextTakerOverride != null;
            }

            /// <summary>
            /// Returns true if the bestiary search box has keyboard focus.
            /// </summary>
            /// <returns>True or false if the check succeeded. Returns null if the check failed.</returns>
            public static bool? IsLocalPlayerSearchingBestiary()
            {
                try
                {
                    FieldInfo _searchBar = typeof(Terraria.GameContent.UI.States.UIBestiaryTest).GetRuntimeFields().Where(x => x.Name == "_searchBar").FirstOrDefault();
                    if (_searchBar != null)
                    {
                        Terraria.GameContent.UI.Elements.UISearchBar _searchBarValue = (Terraria.GameContent.UI.Elements.UISearchBar)_searchBar.GetValue(Terraria.Main.BestiaryUI);
                        FieldInfo isWritingText = _searchBarValue.GetType().GetRuntimeFields().Where(x => x.Name == "isWritingText").FirstOrDefault();
                        if (isWritingText != null)
                        {
                            bool isWritingTextValue = (bool)isWritingText.GetValue(_searchBarValue);
                            return isWritingTextValue && Terraria.Main.CurrentInputTextTakerOverride == _searchBarValue;
                        }
                        else
                            return null;
                    }
                    else
                        return null;
                }
                catch (Exception e)
                {
                    return null;
                }
            }

            /// <summary>
            /// Returns true if the local player is entering text in an input box of some sort and thus should not be activating hotkeys or other keyboard commands.
            /// </summary>
            /// <returns>True or false</returns>
            public static bool IsLocalPlayerTypingSomething()
            {
                return IsLocalPlayerTypingInChat() || IsLocalPlayerRenamingChest() || IsLocalPlayerEditingASign() || IsLocalPlayerTypingInASearchBox();
            }
        }

        #endregion
    }
}
