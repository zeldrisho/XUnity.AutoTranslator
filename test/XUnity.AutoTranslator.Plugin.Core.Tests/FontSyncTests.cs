using System;
using System.IO;
using UnityVietnameseFontSync;
using Xunit;

namespace XUnity.AutoTranslator.Plugin.Core.Tests
{
   public class FontSyncTests
   {
      [Fact]
      public void MatchingRespectsOrderAndExclusions()
      {
         var morty = new FontRule { Pattern = "MortyOld", Match = MatchKind.Contains };
         var feruka = new FontRule { Pattern = "Feruka", Match = MatchKind.Contains };
         var rules = new[] { morty, feruka };
         Assert.Same( morty, FontRules.Select( "mortyold sdf", rules, new[] { "Icons" } ) );
         Assert.Same( feruka, FontRules.Select( "Feruka SDF", rules, new[] { "Icons" } ) );
         Assert.Null( FontRules.Select( "Feruka Icons", rules, new[] { "Icons" } ) );
         Assert.Null( FontRules.Select( "Other", rules, new[] { "Icons" } ) );
      }

      [Fact]
      public void ListsAndFontPathsAreResolvedInOrder()
      {
         Assert.Equal( new[] { "morty", "feruka" }, FontRules.Split( " morty, feruka, morty, " ) );
         var directory = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
         Directory.CreateDirectory( directory );
         try
         {
            var font = Path.Combine( directory, "font.ttf" );
            File.WriteAllText( font, "font" );
            Assert.Equal( font, FontRules.ResolvePath( "font.ttf", "system", directory ) );
            Assert.Equal( Path.Combine( "system", "missing.ttf" ), FontRules.ResolvePath( "missing.ttf", "system", directory ) );
            Assert.Equal( font, FontRules.ResolvePath( font, "system", directory ) );
            Assert.Null( FontRules.ResolvePath( " ", "system", directory ) );
         }
         finally { Directory.Delete( directory, true ); }
      }

      [Fact]
      public void CoverageMustBeVerifiable()
      {
         uint[] requested = { 65, 0x1EF7 };
         Assert.Equal( new uint[] { 0x1EF7 }, FontRules.RemainingCharacters( requested, false, new uint[] { 0x1EF7 } ) );
         Assert.Empty( FontRules.RemainingCharacters( requested, true, null ) );
         Assert.Throws<InvalidOperationException>( () => FontRules.RemainingCharacters( requested, false, null ) );
         Assert.Throws<InvalidOperationException>( () => FontRules.RemainingCharacters( requested, false, new uint[] { 66 } ) );
         Assert.Contains( (uint)'đ', FontRules.Characters( "" ) );
         Assert.Equal( 229, FontRules.Characters( "" ).Length );
         Assert.Throws<ArgumentException>( () => FontRules.Characters( "\uD800" ) );
      }
   }
}
