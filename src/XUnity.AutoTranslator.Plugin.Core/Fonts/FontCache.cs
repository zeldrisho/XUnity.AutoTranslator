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

      public static Font GetOrCreate( int size )
      {
         if( !CachedFonts.TryGetValue( size, out Font font ) )
         {
            font = FontHelper.GetTextFont( size );
            CachedFonts.Add( size, font );
         }
         return font;
      }

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
         if( _hasReadFallbackSystemFont ) return FallbackSystemFontTextMeshPro;
         _hasReadFallbackSystemFont = true;

         if( Settings.FallbackSystemFontName.IsNullOrWhiteSpace() || UnityTypes.TMP_FontAsset_Methods.CreateFontAssetFromFont == null )
            return null;

         try
         {
            var requestedName = Settings.FallbackSystemFontName.Trim();
            var installedNames = FontHelper.GetOSInstalledFontNames();
            if( installedNames == null || !installedNames.Any( x => string.Equals( x, requestedName, StringComparison.OrdinalIgnoreCase ) ) )
            {
               XuaLogger.AutoTranslator.Warn( "The configured fallback system font was not found: " + requestedName );
               return null;
            }

            var font = Font.CreateDynamicFontFromOSFont( requestedName, 90 );
            if( font == null )
            {
               XuaLogger.AutoTranslator.Warn( "The configured fallback system font was not found: " + Settings.FallbackSystemFontName );
               return null;
            }

            FallbackSystemFontTextMeshPro = (UnityEngine.Object)UnityTypes.TMP_FontAsset_Methods.CreateFontAssetFromFont.Invoke( null, new object[] { font } );
            if( FallbackSystemFontTextMeshPro == null )
            {
               XuaLogger.AutoTranslator.Warn( "This TextMeshPro version cannot create a dynamic font asset from an OS font." );
               return null;
            }

            GameObject.DontDestroyOnLoad( font );
            GameObject.DontDestroyOnLoad( FallbackSystemFontTextMeshPro );
            return FallbackSystemFontTextMeshPro;
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Warn( ex, "Unable to create the configured system fallback font: " + Settings.FallbackSystemFontName );
            FallbackSystemFontTextMeshPro = null;
            return null;
         }
      }

      /// <summary>
      /// Adds the configured system font asset to TextMesh Pro's global fallback list.
      /// </summary>
      public static void RegisterFallbackSystemFontTextMeshPro()
      {
         var font = GetOrCreateFallbackSystemFontTextMeshPro();
         if( font == null || UnityTypes.TMP_Settings_Properties.FallbackFontAssets == null ) return;

         try
         {
#if MANAGED
            var fallbacks = UnityTypes.TMP_Settings_Properties.FallbackFontAssets.Get( null ) as IList;
#else
            var fallbacksObj = (Il2CppSystem.Object)UnityTypes.TMP_Settings_Properties.FallbackFontAssets.Get( null );
            fallbacksObj.TryCastTo<Il2CppSystem.Collections.IList>( out var fallbacks);
#endif
            if( fallbacks != null && !fallbacks.Contains( font ) ) fallbacks.Add( font );
         }
         catch( Exception ex )
         {
            XuaLogger.AutoTranslator.Warn( ex, "Unable to register the dynamic system fallback font." );
         }
      }

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
