using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace XUnity.AutoTranslator.Plugin.Core.Web
{
   internal class HttpSecurity
   {
      public readonly HashSet<string> _hosts = new HashSet<string>();

      public void EnableSslFor( params string[] hosts )
      {
         foreach( var host in hosts )
         {
            _hosts.Add( host );
         }
      }

      /// <summary>
      /// Creates a certificate callback that accepts only certificates with no policy errors.
      /// </summary>
      /// <returns>The validation callback, or <see langword="null"/> when no hosts require it.</returns>
      internal RemoteCertificateValidationCallback GetCertificateValidationCheck()
      {
         if( _hosts.Count == 0 ) return null;

         return ( sender, certificate, chain, sslPolicyErrors ) =>
         {
            return sslPolicyErrors == SslPolicyErrors.None;
         };
      }
   }
}
