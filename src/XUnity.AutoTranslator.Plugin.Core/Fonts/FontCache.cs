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
         if( Settings.FallbackSystemFontName.IsNullOrWhiteSpace() || createFontAssetFromFont == null )
         {
            // These are definitive failures, so it is safe to cache the null result.
            _hasReadFallbackSystemFont = true;
            XuaLogger.AutoTranslator.Warn( "[VI-DEBUG] No usable TMP_FontAsset.CreateFontAsset(Font, ...) overload was found; fallback creation is disabled." );
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
               XuaLogger.AutoTranslator.Info( "[VI-DEBUG] Invoking TMP_FontAsset.CreateFontAsset overload: " + createFontAssetFromFont );
               var arguments = UnityTypes.CreateFontAssetFromFontArguments( createFontAssetFromFont, font );
               FallbackSystemFontTextMeshPro = (UnityEngine.Object)createFontAssetFromFont.Invoke( null, arguments );
               XuaLogger.AutoTranslator.Info( "[VI-DEBUG] CreateFontAsset(" + createFontAssetFromFont.GetParameters().Length
                  + " parameters) invocation completed; returned null=" + ( FallbackSystemFontTextMeshPro == null ) + "." );
            }
            catch( Exception ex )
            {
               XuaLogger.AutoTranslator.Error( ex, "[VI-DEBUG] CreateFontAsset(Font, ...) invocation failed. InnerException: "
                  + ( ex.InnerException == null ? "<none>" : ex.InnerException.ToString() ) );
               FallbackSystemFontTextMeshPro = null;
               _hasReadFallbackSystemFont = true;
               return null;
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

            foreach( var character in new[] { 'ế', 'ệ', 'ắ', 'ộ', 'ữ' } )
            {
               XuaLogger.AutoTranslator.Info( "Dynamic TMP fallback HasCharacter U+" + ( (int)character ).ToString( "X4" )
                  + " ('" + character + "'): " + hasCharacter.Invoke( font, new object[] { character, false, false } ) );
            }

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
