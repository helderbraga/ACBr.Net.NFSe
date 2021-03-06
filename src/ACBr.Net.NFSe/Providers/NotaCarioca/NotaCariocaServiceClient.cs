// ***********************************************************************
// Assembly         : ACBr.Net.NFSe
// Author           : RFTD
// Created          : 08-16-2017
//
// Last Modified By : RFTD
// Last Modified On : 08-16-2017
// ***********************************************************************
// <copyright file="NotaCariocaServiceClient.cs" company="ACBr.Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2016 Grupo ACBr.Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Security.Cryptography.X509Certificates;
using ACBr.Net.DFe.Core.Service;

namespace ACBr.Net.NFSe.Providers.NotaCarioca
{
    // ReSharper disable once InconsistentNaming
    internal sealed class NotaCariocaServiceClient : DFeServiceClientBase<INotaCariocaServiceClient>, IABRASFClient
    {
        #region Constructors

        public NotaCariocaServiceClient(string url, TimeSpan? timeOut = null, X509Certificate2 certificado = null) : base(url, timeOut, certificado)
        {
        }

        #endregion Constructors

        #region Methods

        public string RecepcionarLoteRps(string cabec, string msg)
        {
            var request = new RecepcionarLoteRpsRequest(msg);
            var ret = Channel.RecepcionarLoteRps(request);
            return ret.Response;
        }

        public string ConsultarSituacaoLoteRps(string cabec, string msg)
        {
            var request = new ConsultarSituacaoLoteRpsRequest(msg);
            var ret = Channel.ConsultarSituacaoLoteRps(request);
            return ret.Response;
        }

        public string ConsultarNfsePorRps(string cabec, string msg)
        {
            var request = new ConsultarNfsePorRpsRequest(msg);
            var ret = Channel.ConsultarNfsePorRps(request);
            return ret.Response;
        }

        public string ConsultarNfse(string cabec, string msg)
        {
            var request = new ConsultarNfseRequest(msg);
            var ret = Channel.ConsultarNfse(request);
            return ret.Response;
        }

        public string ConsultarLoteRps(string cabec, string msg)
        {
            var request = new ConsultarLoteRpsRequest(msg);
            var ret = Channel.ConsultarLoteRps(request);
            return ret.Response;
        }

        public string CancelarNfse(string cabec, string msg)
        {
            var request = new CancelarNfseRequest(msg);
            var ret = Channel.CancelarNfse(request);
            return ret.Response;
        }

        public string GerarNfse(string cabec, string msg)
        {
            var request = new GerarNfseRequest(msg);
            var ret = Channel.GerarNfse(request);
            return ret.Response;
        }

        #endregion Methods
    }
}