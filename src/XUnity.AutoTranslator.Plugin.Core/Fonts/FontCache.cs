using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core.Configuration;
using XUnity.Common.Constants;
using XUnity.Common.Extensions;
using XUnity.Common.Logging;
using XUnity.Common.Utilities;

namespace XUnity.AutoTranslator.Plugin.Core.Fonts
{
   internal static class FontCache
   {
      private static readonly Dictionary<int, Font> CachedFonts = new Dictionary<int, Font>();
      private static bool _hasReadOverrideFontTextMeshPro = false;
      private static UnityEngine.Object OverrideFontTextMeshPro;
      private static bool _hasReadFallbackFontTextMeshPro = false;
      private static UnityEngine.Object FallbackFontTextMeshPro;
      private static bool _hasReadFallbackSystemFont;
      private static UnityEngine.Object FallbackSystemFontTextMeshPro;

      /// <summary>
      /// Gets a cached default text font for the requested size or creates one.
      /// </summary>
      /// <param name="size">The desired font size.</param>
      /// <returns>The cached or newly created font.</returns>
      public static Font GetOrCreate( int size )
      {
         if( !CachedFonts.TryGetValue( size, out Font font ) )
         {
            font = FontHelper.GetTextFont( size );
            CachedFonts.Add( size, font );
         }
         return font;
      }

      /// <summary>
      /// Gets the cached TextMesh Pro override font or loads it from configuration.
      /// </summary>
      /// <returns>The configured override font asset, or <see langword="null"/> when loading fails.</returns>
      public static object GetOrCreateOverrideFontTextMeshPro()
      {
         if( !_hasReadOverrideFontTextMeshPro )
         {
            try
            {
               _hasReadOverrideFontTextMeshPro = true;
               OverrideFontTextMeshPro = FontHelper.GetTextMeshProFont( Settings.OverrideFontTextMeshPro );
            }
#if IL2CPP
            catch( Exception e ) when( e.ToString().ToLowerInvariant().Contains( "missing" ) || e.ToString().ToLowerInvariant().Contains( "not found" ) )
            {
               XuaLogger.AutoTranslator.Warn( e, "An error occurred while loading text mesh pro override font. Retrying load with custom proxies..." );

               try
               {
                  OverrideFontTextMeshPro = FontHelper.GetTextMeshProFontByCustomProxies( Settings.OverrideFontTextMeshPro );
               }
               catch( Exception ex )
               {
                  XuaLogger.AutoTranslator.Error( ex, "An error occurred while loading text mesh pro override font: " + Settings.OverrideFontTextMeshPro );
               }
            }
#endif
            catch( Exception e )
            {
               XuaLogger.AutoTranslator.Error( e, "An error occurred while loading text mesh pro override font: " + Settings.OverrideFontTextMeshPro );
            }
         }

         return OverrideFontTextMeshPro;
      }

      /// <summary>
      /// Gets the cached TextMesh Pro fallback or creates it from the configured system font.
      /// </summary>
      /// <returns>The fallback font asset, or <see langword="null"/> when it cannot be created.</returns>
      public static UnityEngine.Object GetOrCreateFallbackSystemFontTextMeshPro()
      {
         XuaLogger.AutoTranslator.Info( "[VI-DEBUG] GetOrCreateFallbackSystemFontTextMeshPro: hasRead=" + _hasReadFallbackSystemFont
            + "; cachedNull=" + ( FallbackSystemFontTextMeshPro == null )
            + "; FallbackSystemFontName='" + Settings.FallbackSystemFontName + "'." );
         if( _hasReadFallbackSystemFont )
         {
            XuaLogger.AutoTranslator.Info( "[VI-DEBUG] Returning cached fallback system TMP font: null=" + ( FallbackSystemFontTextMeshPro == null ) + "." );
            return FallbackSystemFontTextMeshPro;
         }
         var createFontAssetFromFont = UnityTypes.TMP_FontAsset_Methods.CreateFontAssetFromFont;
         XuaLogger.AutoTranslator.Info( "[VI-DEBUG] Fallback prerequisites: configured name blank=" + Settings.FallbackSystemFontName.IsNullOrWhiteSpace()
            + "; CreateFontAsset(Font, ...) reflection handle null=" + ( createFontAssetFromFont == null )
            + "; parameter count=" + ( createFontAssetFromFont == null ? 0 : createFontAssetFromFont.GetParameters().Length ) + "." );
         if( Settings.FallbackSystemFontName.IsNullOrWhiteSpace() )
         {
            _hasReadFallbackSystemFont = true;
            XuaLogger.AutoTranslator.Warn( "[VI-DEBUG] No fallback system font name was configured; fallback creation is disabled." );
            return null;
         }

         try
         {
            var requestedName = Settings.FallbackSystemFontName.Trim();
            var installedNames = FontHelper.GetOSInstalledFontNames();
            XuaLogger.AutoTranslator.Info( "[VI-DEBUG] Requested fallback OS font: '" + requestedName + "'; installed font list null=" + ( installedNames == null ) + "." );
            if( installedNames == null || !installedNames.Any( x => string.Equals( x, requestedName, StringComparison.OrdinalIgnoreCase ) ) )
            {
               XuaLogger.AutoTranslator.Warn( "The configured fallback system font was not found: " + requestedName );
               _hasReadFallbackSystemFont = true;
               return null;
            }

            var font = Font.CreateDynamicFontFromOSFont( requestedName, 90 );
            XuaLogger.AutoTranslator.Info( "[VI-DEBUG] Font.CreateDynamicFontFromOSFont returned null=" + ( font == null ) + "." );
            if( font == null )
            {
               XuaLogger.AutoTranslator.Warn( "The configured fallback system font was not found: " + Settings.FallbackSystemFontName );
               _hasReadFallbackSystemFont = true;
               return null;
            }

            try
            {
               if( createFontAssetFromFont != null )
               {
                  XuaLogger.AutoTranslator.Info( "[VI-DEBUG] Invoking TMP_FontAsset.CreateFontAsset overload: " + createFontAssetFromFont );
                  var arguments = UnityTypes.CreateFontAssetFromFontArguments( createFontAssetFromFont, font );
                  FallbackSystemFontTextMeshPro = (UnityEngine.Object)createFontAssetFromFont.Invoke( null, arguments );
                  XuaLogger.AutoTranslator.Info( "[VI-DEBUG] CreateFontAsset(" + createFontAssetFromFont.GetParameters().Length
                     + " parameters) invocation completed; returned null=" + ( FallbackSystemFontTextMeshPro == null ) + "." );
               }

               if( FallbackSystemFontTextMeshPro == null )
                  FallbackSystemFontTextMeshPro = CreateDynamicFallbackFontAsset( font );
            }
            catch( Exception ex )
            {
               XuaLogger.AutoTranslator.Error( ex, "[VI-DEBUG] CreateFontAsset(Font, ...) invocation failed. InnerException: "
                  + ( ex.InnerException == null ? "<none>" : ex.InnerException.ToString() ) );
               FallbackSystemFontTextMeshPro = CreateDynamicFallbackFontAsset( font );
            }

            if( FallbackSystemFontTextMeshPro != null )
            {
               var atlasPopulationMode = UnityTypes.TMP_FontAsset_Properties.AtlasPopulationMode;
               if( atlasPopulationMode != null && atlasPopulationMode.PropertyType.IsEnum )
               {
                  try
                  {
                     atlasPopulationMode.Set( FallbackSystemFontTextMeshPro,
                        Enum.Parse( atlasPopulationMode.PropertyType, "Dynamic", true ) );
                  }
                  catch( Exception ex )
                  {
                     XuaLogger.AutoTranslator.Warn( ex, "Unable to set dynamic TMP atlas population mode." );
                  }
               }
            }

            LogFallbackSystemFontDiagnostics( FallbackSystemFontTextMeshPro );
            if( FallbackSystemFontTextMeshPro == null )
            {
               _hasReadFallbackSystemFont = true;
               XuaLogger.AutoTranslator.Warn( "This TextMeshPro version cannot create a dynamic font asset from an OS font." );
               return null;
            }

            _hasReadFallbackSystemFont = true;
            GameObject.DontDestroyOnLoad( font );
            GameObject.DontDestroyOnLoad( FallbackSystemFontTextMeshPro );
            return FallbackSystemFontTextMeshPro;
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Error( ex, "[VI-DEBUG] Unable to create the configured system fallback font. InnerException: "
               + ( ex.InnerException == null ? "<none>" : ex.InnerException.ToString() ) );
            FallbackSystemFontTextMeshPro = null;
            _hasReadFallbackSystemFont = true;
            return null;
         }
      }

      private static UnityEngine.Object CreateDynamicFallbackFontAsset( Font font )
      {
         XuaLogger.AutoTranslator.Info( "[VI-DEBUG] CreateFontAsset returned null; attempting manual TMP_FontAsset ScriptableObject construction." );
         try
         {
            if( UnityTypes.TMP_FontAsset?.ClrType == null )
            {
               XuaLogger.AutoTranslator.Warn( "[VI-DEBUG] Manual TMP font construction skipped: TMP_FontAsset type was not resolved." );
               return null;
            }

#if IL2CPP
            var asset = ScriptableObject.CreateInstance( UnityTypes.TMP_FontAsset.UnityType ) as UnityEngine.Object;
#else
            var asset = ScriptableObject.CreateInstance( UnityTypes.TMP_FontAsset.ClrType ) as UnityEngine.Object;
#endif
            XuaLogger.AutoTranslator.Info( "[VI-DEBUG] ScriptableObject.CreateInstance(TMP_FontAsset) returned null=" + ( asset == null ) + "." );
            if( asset == null ) return null;

            SetSourceFontFile( asset, font );

            var atlasPopulationMode = UnityTypes.TMP_FontAsset_Properties.AtlasPopulationMode;
            if( atlasPopulationMode != null && atlasPopulationMode.PropertyType.IsEnum )
               atlasPopulationMode.Set( asset, Enum.ToObject( atlasPopulationMode.PropertyType, 1 ) );

            var multiAtlas = UnityTypes.TMP_FontAsset_Properties.IsMultiAtlasTexturesEnabled;
            var multiAtlasField = UnityTypes.TMP_FontAsset_Properties.IsMultiAtlasTexturesEnabledField;
            if( multiAtlas != null ) multiAtlas.Set( asset, true );
            else if( multiAtlasField != null ) multiAtlasField.Set( asset, true );
            else XuaLogger.AutoTranslator.Info( "[VI-DEBUG] TMP_FontAsset.isMultiAtlasTexturesEnabled was not resolved; continuing." );

            BootstrapDynamicFontAsset( asset, font );

            var readDefinition = UnityTypes.TMP_FontAsset_Methods.ReadFontAssetDefinition;
            if( readDefinition != null )
            {
               try
               {
                  XuaLogger.AutoTranslator.Info( "[VI-DEBUG] Calling TMP_FontAsset.ReadFontAssetDefinition()." );
                  readDefinition.Invoke( asset );
               }
               catch( Exception ex )
               {
                  XuaLogger.AutoTranslator.Warn( ex, "[VI-DEBUG] TMP_FontAsset.ReadFontAssetDefinition() failed; retaining manually initialized asset." );
               }
            }
            else XuaLogger.AutoTranslator.Info( "[VI-DEBUG] TMP_FontAsset.ReadFontAssetDefinition() was not resolved; continuing." );

            // ReadFontAssetDefinition may clear the serialized source-font
            // state. Restore it and load the TextCore face immediately before
            // AddCharacters/TryAddCharacters is called.
            SetSourceFontFile( asset, font );
            PopulateFaceInfo( asset, font );

            // ReadFontAssetDefinition can rebuild the tables, but some TMP
            // versions only do so when their private initialization helpers are
            // called explicitly after a manually-created asset.
            try
            {
               UnityTypes.TMP_FontAsset_Methods.InitializeGlyphLookupDictionary?.Invoke( asset );
               UnityTypes.TMP_FontAsset_Methods.InitializeCharacterLookupDictionary?.Invoke( asset );
            }
            catch( Exception ex )
            {
               XuaLogger.AutoTranslator.Warn( ex, "[VI-DEBUG] TMP lookup dictionary initialization failed." );
            }

            // AddCharacters is the supported initialization path in newer TMP
            // versions.  It also exercises the dynamic atlas code, unlike the
            // legacy ReadFontAssetDefinition call.
            SeedDynamicFontAsset( asset, "e" );

            asset.name = Settings.FallbackSystemFontName + " Dynamic Fallback";
            XuaLogger.AutoTranslator.Info( "[VI-DEBUG] Manual TMP_FontAsset construction completed; name='" + asset.name + "'." );
            return asset;
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Warn( ex, "[VI-DEBUG] Manual TMP_FontAsset construction failed." );
            return null;
         }
      }

      private static void BootstrapDynamicFontAsset( UnityEngine.Object asset, Font sourceFont )
      {
         const int atlasSize = 1024;
         try
         {
            // Match the version written by TMP_FontAsset.CreateFontAsset so
            // ReadFontAssetDefinition does not enter the legacy upgrade path.
            SetField( UnityTypes.TMP_FontAsset_Properties.VersionField, asset, "1.1.0" );
            SetCollection( UnityTypes.TMP_FontAsset_Properties.CharacterTable, asset );
            SetCollection( UnityTypes.TMP_FontAsset_Properties.GlyphTable, asset );
            SetCollection( UnityTypes.TMP_FontAsset_Properties.CharacterLookupTable, asset );
            SetCollection( UnityTypes.TMP_FontAsset_Properties.GlyphLookupTable, asset );
            SetCollection( UnityTypes.TMP_FontAsset_Properties.FontFeatureTable, asset );
            SetCollection( UnityTypes.TMP_FontAsset_Properties.FreeGlyphRects, asset );
            SetCollection( UnityTypes.TMP_FontAsset_Properties.UsedGlyphRects, asset );
            SetEmptyValue( UnityTypes.TMP_FontAsset_Properties.AtlasTextureBuffer, asset );

            // ReadFontAssetDefinition dereferences material even for an empty
            // asset (to read the gradient scale and material hash).
            var material = UnityTypes.TMP_FontAsset_Properties.Material;
            if( material != null && material.Get( asset ) == null )
            {
               var shader = Shader.Find( "UI/Default" );
               if( shader != null ) material.Set( asset, new Material( shader ) );
            }

            var texture = new Texture2D( atlasSize, atlasSize, TextureFormat.Alpha8, false );
            texture.name = "XUnity AutoTranslator Dynamic TMP Atlas";
            SetAtlas( UnityTypes.TMP_FontAsset_Properties.AtlasTextures, asset, texture );
            SetField( UnityTypes.TMP_FontAsset_Properties.AtlasTexture, asset, texture );
            SetField( UnityTypes.TMP_FontAsset_Properties.AtlasWidth, asset, atlasSize );
            SetField( UnityTypes.TMP_FontAsset_Properties.AtlasHeight, asset, atlasSize );
            PopulateFaceInfo( asset, sourceFont );

            // Dynamic atlas packing starts with one free rectangle.  These
            // fields are not initialized by ScriptableObject.CreateInstance.
            AddFreeGlyphRect( UnityTypes.TMP_FontAsset_Properties.FreeGlyphRects, asset, atlasSize );

            XuaLogger.AutoTranslator.Info( "[VI-DEBUG] Bootstrapped TMP tables and atlas; atlasTextures valid="
               + HasValidAtlasTexture( UnityTypes.TMP_FontAsset_Properties.AtlasTextures?.Get( asset ) ) + "." );
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Warn( ex, "[VI-DEBUG] Unable to bootstrap one or more TMP font asset internals." );
         }
      }

      private static bool HasValidAtlasTexture( object textures )
      {
         if( textures is Array array )
            return array.Length > 0 && array.GetValue( 0 ) != null;
         if( textures is IEnumerable enumerable )
         {
            foreach( var texture in enumerable ) return texture != null;
         }
         return false;
      }

      private static void SetCollection( CachedField field, object asset )
      {
         if( field == null || field.FieldType == null || field.Get( asset ) != null ) return;
         field.Set( asset, Activator.CreateInstance( field.FieldType ) );
      }

      private static void SetEmptyValue( CachedField field, object asset )
      {
         if( field == null || field.FieldType == null || field.Get( asset ) != null ) return;
         var value = field.FieldType.IsArray
            ? Array.CreateInstance( field.FieldType.GetElementType(), 0 )
            : Activator.CreateInstance( field.FieldType );
         field.Set( asset, value );
      }

      private static void SetAtlas( CachedField field, object asset, Texture2D texture )
      {
         if( field == null || field.FieldType == null ) return;
         var type = field.FieldType;
         if( type.IsArray )
         {
            var array = Array.CreateInstance( type.GetElementType(), 1 );
            array.SetValue( texture, 0 );
            field.Set( asset, array );
         }
         else
         {
            var list = Activator.CreateInstance( type );
            var add = type.GetMethod( "Add" );
            if( add != null ) add.Invoke( list, new object[] { texture } );
            field.Set( asset, list );
         }
      }

      private static void AddFreeGlyphRect( CachedField field, object asset, int atlasSize )
      {
         if( field == null ) return;
         var collection = field.Get( asset );
         if( collection == null ) return;
         var add = collection.GetType().GetMethod( "Add" );
         if( add == null ) return;

         var rectType = add.GetParameters()[ 0 ].ParameterType;
         var rect = Activator.CreateInstance( rectType, new object[] { 0, 0, atlasSize - 1, atlasSize - 1 } );
         add.Invoke( collection, new[] { rect } );
      }

      private static void SeedDynamicFontAsset( UnityEngine.Object asset, string characters )
      {
         try
         {
            var method = UnityTypes.TMP_FontAsset_Methods.AddCharacters
               ?? UnityTypes.TMP_FontAsset_Methods.TryAddCharacters;
            if( method == null ) return;

            var parameters = method.GetParameters();
            var arguments = new object[ parameters.Length ];
            arguments[ 0 ] = characters;
            for( var i = 1; i < parameters.Length; i++ )
            {
               var parameter = parameters[ i ];
               arguments[ i ] = parameter.IsOut ? null
                  : parameter.ParameterType.IsValueType ? Activator.CreateInstance( parameter.ParameterType ) : null;
            }

            var result = method.Invoke( asset, arguments );
            XuaLogger.AutoTranslator.Info( "[VI-DEBUG] TMP " + method.Name + "('" + characters
               + "') returned " + ( result ?? "<void>" ) + "." );
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Warn( ex, "[VI-DEBUG] TMP dynamic character seeding failed." );
         }
      }

      private static void SetField( CachedField field, object asset, object value )
      {
         if( field != null ) field.Set( asset, value );
      }

      private static void SetSourceFontFile( UnityEngine.Object asset, Font sourceFont )
      {
         var property = UnityTypes.TMP_FontAsset_Properties.SourceFontFile;
         var field = UnityTypes.TMP_FontAsset_Properties.SourceFontFileField;
         try
         {
            // Set both where available. On some TMP builds the property setter
            // does not update the serialized m_SourceFontFile backing field.
            property?.Set( asset, sourceFont );
            field?.Set( asset, sourceFont );
            var value = property?.Get( asset ) ?? field?.Get( asset );
            XuaLogger.AutoTranslator.Info( "[VI-DEBUG] TMP sourceFontFile assigned; readback null=" + ( value == null ) + "." );
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Warn( ex, "[VI-DEBUG] Unable to assign TMP sourceFontFile." );
         }
      }

      private static void PopulateFaceInfo( object asset, Font sourceFont )
      {
         var faceInfo = UnityTypes.TMP_FontAsset_Properties.FaceInfo;
         var get = UnityTypes.FontEngine_Methods.GetFaceInfo;
         if( faceInfo == null || get == null ) return;

         var loaded = false;
         var load = UnityTypes.FontEngine_Methods.LoadFontFace;
         if( load != null )
            loaded = InvokeLoadFontFace( load, sourceFont, 90 );

         // Unity 2022's LowLevel FontEngine also exposes a file-path overload.
         // Prefer the Font overload, but try the actual file for runtimes where
         // Font.CreateDynamicFontFromOSFont does not provide TextCore data.
         if( !loaded )
         {
            var pathLoad = UnityTypes.FontEngine_Methods.LoadFontFaceFromPath;
            foreach( var path in GetFontFileCandidates() )
            {
               if( pathLoad == null || !File.Exists( path ) ) continue;
               if( InvokeLoadFontFace( pathLoad, path, 90 ) )
               {
                  loaded = true;
                  XuaLogger.AutoTranslator.Info( "[VI-DEBUG] FontEngine.LoadFontFace(path) succeeded: " + path );
                  break;
               }
            }
         }

         XuaLogger.AutoTranslator.Info( "[VI-DEBUG] FontEngine face initialization result: " + loaded
            + "; Font overload resolved=" + ( load != null )
            + "; path overload resolved=" + ( UnityTypes.FontEngine_Methods.LoadFontFaceFromPath != null ) + "." );
         if( loaded )
         {
            faceInfo.Set( asset, get.Invoke( null, null ) );
            var initialized = UnityTypes.TMP_FontAsset_Properties.SourceFontFileInitialized;
            if( initialized != null && initialized.FieldType == typeof( bool ) ) initialized.Set( asset, true );
            XuaLogger.AutoTranslator.Info( "[VI-DEBUG] TMP m_SourceFontFile_Initialized set to true; field resolved="
               + ( initialized != null ) + "; bool=" + ( initialized != null && initialized.FieldType == typeof( bool ) ) + "." );
         }
      }

      private static bool InvokeLoadFontFace( System.Reflection.MethodInfo method, object source, int pointSize )
      {
         try
         {
            var parameters = method.GetParameters();
            var args = new object[ parameters.Length ];
            args[ 0 ] = source;
            var intArgument = 0;
            for( var i = 1; i < args.Length; i++ )
            {
               if( parameters[ i ].ParameterType == typeof( int ) )
                  args[ i ] = intArgument++ == 0 ? (object)pointSize : 0;
               else
                  args[ i ] = Activator.CreateInstance( parameters[ i ].ParameterType );
            }
            var result = method.Invoke( null, args );
            return result == null || Convert.ToBoolean( result );
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Warn( ex, "[VI-DEBUG] FontEngine.LoadFontFace failed for " + source + "." );
            return false;
         }
      }

      private static IEnumerable<string> GetFontFileCandidates()
      {
         var fonts = Environment.GetFolderPath( Environment.SpecialFolder.Fonts );
         if( fonts.IsNullOrWhiteSpace() ) yield break;
         var name = Settings.FallbackSystemFontName ?? "";
         var compact = new string( name.Where( char.IsLetterOrDigit ).ToArray() );
         foreach( var extension in new[] { ".ttf", ".otf" } )
         {
            var candidate = Path.Combine( fonts, compact + extension );
            if( File.Exists( candidate ) ) yield return candidate;
         }
      }

      private static void LogFallbackSystemFontDiagnostics( UnityEngine.Object font )
      {
         try
         {
            XuaLogger.AutoTranslator.Info( "Dynamic TMP fallback CreateFontAsset(Font) method resolved/executed: true; returned null: " + ( font == null ) );
            if( font == null ) return;

            var atlasPopulationMode = UnityTypes.TMP_FontAsset_Properties.AtlasPopulationMode;
            XuaLogger.AutoTranslator.Info( "Dynamic TMP fallback atlasPopulationMode: "
               + ( atlasPopulationMode == null ? "<unavailable>" : ( atlasPopulationMode.Get( font )?.ToString() ?? "<null>" ) ) );

            var hasCharacter = UnityTypes.TMP_FontAsset_Methods.HasCharacter;
            if( hasCharacter == null )
            {
               XuaLogger.AutoTranslator.Warn( "Dynamic TMP fallback HasCharacter(char, bool, bool) was not resolved." );
               return;
            }

            foreach( var character in new[] { 'e', 'ế', 'ệ', 'ắ', 'ộ', 'ữ' } )
            {
               XuaLogger.AutoTranslator.Info( "Dynamic TMP fallback HasCharacter U+" + ( (int)character ).ToString( "X4" )
                  + " ('" + character + "'): " + hasCharacter.Invoke( font, new object[] { character, false, false } ) );
            }

            XuaLogger.AutoTranslator.Info( "Dynamic TMP fallback atlasTextures has at least one valid texture: "
               + HasValidAtlasTexture( UnityTypes.TMP_FontAsset_Properties.AtlasTextures?.Get( font ) ) );

            var decomposed = "e\u0301";
            foreach( var character in decomposed )
            {
               XuaLogger.AutoTranslator.Info( "Dynamic TMP fallback HasCharacter NFD U+" + ( (int)character ).ToString( "X4" )
                  + " ('" + character + "'): " + hasCharacter.Invoke( font, new object[] { character, false, false } ) );
            }
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Warn( ex, "Unable to log dynamic TMP fallback diagnostics." );
         }
      }

      /// <summary>
      /// Adds the configured system font asset to TextMesh Pro's global fallback list.
      /// </summary>
      public static void RegisterFallbackSystemFontTextMeshPro()
      {
         var font = GetOrCreateFallbackSystemFontTextMeshPro();
         if( font == null )
         {
            XuaLogger.AutoTranslator.Warn( "Dynamic TMP fallback registration skipped because the fallback asset is null." );
            return;
         }
         if( UnityTypes.TMP_Settings_Properties.FallbackFontAssets == null )
         {
            XuaLogger.AutoTranslator.Warn( "Dynamic TMP fallback registration skipped because TMP_Settings.fallbackFontAssets was not resolved." );
            return;
         }

         try
         {
#if MANAGED
            var fallbacks = UnityTypes.TMP_Settings_Properties.FallbackFontAssets.Get( null ) as IList;
#else
            var fallbacksObj = (Il2CppSystem.Object)UnityTypes.TMP_Settings_Properties.FallbackFontAssets.Get( null );
            fallbacksObj.TryCastTo<Il2CppSystem.Collections.IList>( out var fallbacks);
#endif
            if( fallbacks == null )
            {
               XuaLogger.AutoTranslator.Warn( "Dynamic TMP fallback registration failed: TMP_Settings.fallbackFontAssets is null." );
               return;
            }

            if( !fallbacks.Contains( font ) ) fallbacks.Add( font );
            XuaLogger.AutoTranslator.Info( "Dynamic TMP fallback global registration succeeded: " + fallbacks.Contains( font ) );
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Warn( ex, "Unable to register the dynamic system fallback font." );
         }
      }

      /// <summary>
      /// Gets the cached TextMesh Pro fallback font or loads it from configuration.
      /// </summary>
      /// <returns>The configured fallback font asset, or <see langword="null"/> when loading fails.</returns>
      public static UnityEngine.Object GetOrCreateFallbackFontTextMeshPro()
      {
         if( !_hasReadFallbackFontTextMeshPro )
         {
            try
            {
               _hasReadFallbackFontTextMeshPro = true;
               FallbackFontTextMeshPro = FontHelper.GetTextMeshProFont( Settings.FallbackFontTextMeshPro );
            }
#if IL2CPP
            catch( Exception e ) when( e.ToString().ToLowerInvariant().Contains( "missing" ) || e.ToString().ToLowerInvariant().Contains( "not found" ) )
            {
               XuaLogger.AutoTranslator.Warn( e, "An error occurred while loading text mesh pro fallback font. Retrying load with custom proxies..." );

               try
               {
                  FallbackFontTextMeshPro = FontHelper.GetTextMeshProFontByCustomProxies( Settings.FallbackFontTextMeshPro );
               }
               catch( Exception ex )
               {
                  XuaLogger.AutoTranslator.Error( ex, "An error occurred while loading text mesh pro fallback font: " + Settings.FallbackFontTextMeshPro );
               }
            }
#endif
            catch( Exception e )
            {
               XuaLogger.AutoTranslator.Error( e, "An error occurred while loading text mesh pro fallback font: " + Settings.FallbackFontTextMeshPro );
            }
         }

         return FallbackFontTextMeshPro;
      }
   }
}
