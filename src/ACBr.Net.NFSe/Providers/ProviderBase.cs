// ***********************************************************************
// Assembly         : ACBr.Net.NFe
// Author           : RFTD
// Created          : 07-27-2014
//
// Last Modified By : RFTD
// Last Modified On : 09-28-2014
// ***********************************************************************
// <copyright file="ProviderBase.cs" company="ACBr.Net">
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

using ACBr.Net.Core.Exceptions;
using ACBr.Net.Core.Extensions;
using ACBr.Net.Core.Logging;
using ACBr.Net.DFe.Core;
using ACBr.Net.DFe.Core.Common;
using ACBr.Net.DFe.Core.Serializer;
using ACBr.Net.NFSe.Configuracao;
using ACBr.Net.NFSe.Nota;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using ACBr.Net.DFe.Core.Document;

namespace ACBr.Net.NFSe.Providers
{
    /// <summary>
    /// Class ProviderBase.
    /// </summary>
    public abstract class ProviderBase : IACBrLog, IDisposable
    {
        #region Internal Types

        protected enum TipoArquivo
        {
            Webservice,
            Rps,
            NFSe
        }

        protected enum Ocorrencia
        {
            NaoObrigatoria,
            Obrigatoria,
            MaiorQueZero
        }

        #endregion Internal Types

        #region Fields

        private X509Certificate2 certificado;
        private bool disposed;

        #endregion Fields

        #region Constantes

        /// <summary>
        /// The er r_ ms g_ maior
        /// </summary>
        public const string ErrMsgMaior = "Tamanho maior que o m�ximo permitido";

        /// <summary>
        /// The er r_ ms g_ menor
        /// </summary>
        public const string ErrMsgMenor = "Tamanho menor que o m�nimo permitido";

        /// <summary>
        /// The er r_ ms g_ vazio
        /// </summary>
        public const string ErrMsgVazio = "Nenhum valor informado";

        /// <summary>
        /// The er r_ ms g_ invalido
        /// </summary>
        public const string ErrMsgInvalido = "Conte�do inv�lido";

        /// <summary>
        /// The er r_ ms g_ maxim o_ decimais
        /// </summary>
        public const string ErrMsgMaximoDecimais = "Numero m�ximo de casas decimais permitidas";

        /// <summary>
        /// The er r_ ms g_ maio r_ maximo
        /// </summary>
        public const string ErrMsgMaiorMaximo = "N�mero de ocorr�ncias maior que o m�ximo permitido - M�ximo ";

        /// <summary>
        /// The er r_ ms g_ fina l_ meno r_ inicial
        /// </summary>
        public const string ErrMsgFinalMenorInicial = "O numero final n�o pode ser menor que o inicial";

        /// <summary>
        /// The er r_ ms g_ arquiv o_ na o_ encontrado
        /// </summary>
        public const string ErrMsgArquivoNaoEncontrado = "Arquivo n�o encontrado";

        /// <summary>
        /// The er r_ ms g_ soment e_ um
        /// </summary>
        public const string ErrMsgSomenteUm = "Somente um campo deve ser preenchido";

        /// <summary>
        /// The er r_ ms g_ meno r_ minimo
        /// </summary>
        public const string ErrMsgMenorMinimo = "N�mero de ocorr�ncias menor que o m�nimo permitido - M�nimo ";

        /// <summary>
        /// The ds c_ CNPJ
        /// </summary>
        public const string DscCnpj = "CNPJ(MF)";

        /// <summary>
        /// The ds c_ CPF
        /// </summary>
        public const string DscCpf = "CPF";

        #endregion Constantes

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderBase"/> class.
        /// </summary>
        protected ProviderBase(ConfiguracoesNFSe config, ACBrMunicipioNFSe municipio)
        {
            Name = "Base";
            ListaDeAlertas = new List<string>();
            FormatoAlerta = "TAG:%TAG% ID:%ID%/%TAG%(%DESCRICAO%) - %MSG%.";
            Config = config;
            Municipio = municipio;
        }

        /// <inheritdoc />
        ~ProviderBase()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        #endregion Constructor

        #region Propriedades

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the lista de alertas.
        /// </summary>
        /// <value>The lista de alertas.</value>
        public List<string> ListaDeAlertas { get; }

        /// <summary>
        /// Gets or sets the formato alerta.
        /// </summary>
        /// <value>The formato alerta.</value>
        public string FormatoAlerta { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [retirar acentos].
        /// </summary>
        /// <value><c>true</c> if [retirar acentos]; otherwise, <c>false</c>.</value>
        public bool RetirarAcentos { get; set; }

        public ConfiguracoesNFSe Config { get; }

        public ACBrMunicipioNFSe Municipio { get; }

        public TimeSpan? TimeOut
        {
            get
            {
                TimeSpan? timeOut = null;
                if (Config.WebServices.AjustaAguardaConsultaRet)
                    timeOut = TimeSpan.FromSeconds((int)Config.WebServices.AguardarConsultaRet);

                return timeOut;
            }
        }

        public X509Certificate2 Certificado => certificado ?? (certificado = Config.Certificados.ObterCertificado());

        #endregion Propriedades

        #region Methods

        #region Public

        /// <summary>
        /// Carrega o xml da NFSe/Rps.
        /// </summary>
        /// <param name="xml">Local do arquivo Xml</param>
        /// <param name="encoding">Enconde para utilizar na leitura do arquivo</param>
        /// <returns></returns>
        public NotaFiscal LoadXml(string xml, Encoding encoding = null)
        {
            Guard.Against<ArgumentNullException>(xml.IsEmpty(), "Xml n�o pode ser vazio ou nulo");

            XDocument doc;
            if (File.Exists(xml))
            {
                if (encoding == null)
                {
                    doc = XDocument.Load(xml);
                }
                else
                {
                    using (var sr = new StreamReader(xml, encoding))
                    {
                        doc = XDocument.Load(sr);
                    }
                }
            }
            else
            {
                doc = XDocument.Parse(xml);
            }

            return LoadXml(doc);
        }

        /// <summary>
        /// Carrega o xml da NFSe/Rps.
        /// </summary>
        /// <param name="stream">Stream contendo os dados do arquivo xml.</param>
        /// <returns></returns>
        public NotaFiscal LoadXml(Stream stream)
        {
            Guard.Against<ArgumentNullException>(stream == null, "Stream n�o pode ser nulo !");

            var doc = XDocument.Load(stream);
            return LoadXml(doc);
        }

        /// <summary>
        /// Carrega o xml da NFSe/Rps.
        /// </summary>
        /// <param name="xml">Classe XDocument com um xml carregado.</param>
        /// <returns></returns>
        public virtual NotaFiscal LoadXml(XDocument xml)
        {
            throw new NotImplementedException("LoadXml");
        }

        /// <summary>
        /// Retorna o xml da Rps em formato string.
        /// </summary>
        /// <param name="nota"></param>
        /// <param name="identado"></param>
        /// <param name="showDeclaration"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual string GetXmlRps(NotaFiscal nota, bool identado = true, bool showDeclaration = true)
        {
            throw new NotImplementedException("GetXmlRps");
        }

        /// <summary>
        /// Retorna o xml da NFSe em formato string.
        /// </summary>
        /// <param name="nota"></param>
        /// <param name="identado"></param>
        /// <param name="showDeclaration"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual string GetXmlNFSe(NotaFiscal nota, bool identado = true, bool showDeclaration = true)
        {
            throw new NotImplementedException("GetXmlNFSe");
        }

        /// <summary>
        /// Enviar cole��o de Rps para o provedor de forma assincrona.
        /// </summary>
        /// <param name="lote"></param>
        /// <param name="notas"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual RetornoWebservice Enviar(int lote, NotaFiscalCollection notas)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        /// <summary>
        /// Enviar cole��o de Rps para o provedor de forma sincrona.
        /// </summary>
        /// <param name="lote"></param>
        /// <param name="notas"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual RetornoWebservice EnviarSincrono(int lote, NotaFiscalCollection notas)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        /// <summary>
        /// Consulta a situa��o do lote.
        /// </summary>
        /// <param name="lote"></param>
        /// <param name="protocolo"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual RetornoWebservice ConsultarSituacao(int lote, string protocolo)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        /// <summary>
        /// Consultar o lote de rps.
        /// </summary>
        /// <param name="lote"></param>
        /// <param name="protocolo"></param>
        /// <param name="notas"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual RetornoWebservice ConsultarLoteRps(int lote, string protocolo, NotaFiscalCollection notas)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        public virtual RetornoWebservice ConsultarSequencialRps(string serie)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        public virtual RetornoWebservice ConsultaNFSeRps(string numero, string serie, TipoRps tipo, NotaFiscalCollection notas)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        public virtual RetornoWebservice ConsultaNFSe(DateTime? inicio, DateTime? fim, string numeroNfse, int pagina, string cnpjTomador,
            string imTomador, string nomeInter, string cnpjInter, string imInter, string serie, NotaFiscalCollection notas)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        public virtual RetornoWebservice CancelaNFSe(string codigoCancelamento, string numeroNFSe, string motivo, NotaFiscalCollection notas)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        public virtual RetornoWebservice CancelaNFSe(int lote, NotaFiscalCollection notas)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        public virtual RetornoWebservice SubstituirNFSe(string codigoCancelamento, string numeroNFSe, string motivo, NotaFiscalCollection notas)
        {
            throw new NotImplementedException("Fun��o n�o implementada/suportada neste Provedor !");
        }

        #endregion Public

        #region Protected

        /// <summary>
        /// Retorna a URL para o tipo de servi�o.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        protected string GetUrl(TipoUrl url)
        {
            switch (Config.WebServices.Ambiente)
            {
                case DFeTipoAmbiente.Producao:
                    return Municipio.UrlProducao[url];

                default:
                    return Municipio.UrlHomologacao[url];
            }
        }

        /// <summary>
        /// Retornar o XML da assinatura ou nulo caso n�o tenha nada.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>XElement.</returns>
        protected XElement WriteSignature(DFeSignature signature)
        {
            if (signature.SignatureValue.IsEmpty() || signature.SignedInfo.Reference.DigestValue.IsEmpty() ||
                signature.KeyInfo.X509Data.X509Certificate.IsEmpty())
                return null;

            var ms = new MemoryStream();
            var serializer = DFeSerializer.CreateSerializer<DFeSignature>();
            if (!serializer.Serialize(signature, ms)) return null;

            return XElement.Load(ms);
        }

        /// <summary>
        /// Carrega a assinatura do Xml.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>DFeSignature.</returns>
        protected DFeSignature LoadSignature(XElement element)
        {
            if (element == null) return new DFeSignature();

            var serializer = DFeSerializer.CreateSerializer<DFeSignature>();
            return serializer.Deserialize(element.ToString());
        }

        /// <summary>
        /// Adicionars the tag CNPJCPF.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="tagCPF">The i d1.</param>
        /// <param name="tagCNPJ">The i d2.</param>
        /// <param name="valor">The CNPJCPF.</param>
        /// <param name="ns"></param>
        /// <returns>XmlElement.</returns>
        protected XElement AdicionarTagCNPJCPF(string id, string tagCPF, string tagCNPJ, string valor, XNamespace ns = null)
        {
            valor = valor.Trim().OnlyNumbers();

            XElement tag = null;
            switch (valor.Length)
            {
                case 11:
                    tag = AdicionarTag(TipoCampo.StrNumber, id, tagCPF, 11, 11, Ocorrencia.Obrigatoria, valor, string.Empty, ns);
                    if (!valor.IsCPF())
                        WAlerta(tagCPF, "CPF", "CPF", ErrMsgInvalido);
                    break;

                case 14:
                    tag = AdicionarTag(TipoCampo.StrNumber, id, tagCNPJ, 14, 14, Ocorrencia.Obrigatoria, valor, string.Empty, ns);
                    if (!valor.IsCNPJ())
                        WAlerta(tagCNPJ, "CNPJ", "CNPJ", ErrMsgInvalido);
                    break;
            }

            if (!valor.Length.IsIn(11, 14))
                WAlerta($"{tagCPF}-{tagCNPJ}", "CNPJ-CPF", "CNPJ/CPF", ErrMsgVazio);

            return tag;
        }

        /// <summary>
        /// Adicionars the tag.
        /// </summary>
        /// <param name="tipo">The tipo.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="ocorrencia">The ocorrencia.</param>
        /// <param name="valor">The valor.</param>
        /// <param name="descricao">The descricao.</param>
        /// <param name="ns"></param>
        /// <param name="nsAtt"></param>
        /// <returns>XmlElement.</returns>
        protected XElement AdicionarTag(TipoCampo tipo, string id, string tag, XNamespace ns, int min, int max, Ocorrencia ocorrencia, object valor, string descricao = "")
        {
            Guard.Against<ArgumentException>(ns == null, "Namespace n�o informado");

            return AdicionarTag(tipo, id, tag, min, max, ocorrencia, valor, descricao, ns);
        }

        /// <summary>
        /// Adicionars the tag.
        /// </summary>
        /// <param name="tipo">The tipo.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="ocorrencia">The ocorrencia.</param>
        /// <param name="valor">The valor.</param>
        /// <param name="descricao">The descricao.</param>
        /// <returns>XmlElement.</returns>
        protected XElement AdicionarTag(TipoCampo tipo, string id, string tag, int min, int max, Ocorrencia ocorrencia, object valor, string descricao = "")
        {
            return AdicionarTag(tipo, id, tag, min, max, ocorrencia, valor, descricao, null);
        }

        private XElement AdicionarTag(TipoCampo tipo, string id, string tag, int min, int max, Ocorrencia ocorrencia, object valor, string descricao, XNamespace ns)
        {
            try
            {
                var conteudoProcessado = string.Empty;
                var estaVazio = valor == null || valor.ToString().IsEmpty();

                if (!estaVazio)
                {
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (tipo)
                    {
                        case TipoCampo.Str:
                            conteudoProcessado = valor.ToString().Trim();
                            break;

                        case TipoCampo.Dat:
                        case TipoCampo.DatCFe:
                            DateTime data;
                            if (DateTime.TryParse(valor.ToString(), out data))
                            {
                                conteudoProcessado = data.ToString(tipo == TipoCampo.DatCFe ? "yyyyMMdd" : "yyyy-MM-dd");
                            }
                            else
                            {
                                estaVazio = true;
                            }
                            break;

                        case TipoCampo.Hor:
                        case TipoCampo.HorCFe:
                            DateTime hora;
                            if (DateTime.TryParse(valor.ToString(), out hora))
                            {
                                conteudoProcessado = hora.ToString(tipo == TipoCampo.HorCFe ? "HHmmss" : "HH:mm:ss");
                            }
                            else
                            {
                                estaVazio = true;
                            }
                            break;

                        case TipoCampo.DatHor:
                            DateTime dthora;
                            if (DateTime.TryParse(valor.ToString(), out dthora))
                                conteudoProcessado = dthora.ToString("s");
                            else
                                estaVazio = true;
                            break;

                        case TipoCampo.DatHorTz:
                            DateTime dthoratz;
                            if (DateTime.TryParse(valor.ToString(), out dthoratz))
                            {
                                conteudoProcessado = dthoratz.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'sszzz");
                            }
                            else
                            {
                                estaVazio = true;
                            }
                            break;

                        case TipoCampo.De2:
                        case TipoCampo.De3:
                        case TipoCampo.De4:
                        case TipoCampo.De6:
                        case TipoCampo.De10:
                            var numberFormat = CultureInfo.InvariantCulture.NumberFormat;
                            decimal vDecimal;
                            if (decimal.TryParse(valor.ToString(), out vDecimal))
                            {
                                if (ocorrencia == Ocorrencia.MaiorQueZero && vDecimal == 0)
                                {
                                    estaVazio = true;
                                }
                                else
                                {
                                    // ReSharper disable once SwitchStatementMissingSomeCases
                                    switch (tipo)
                                    {
                                        case TipoCampo.De2:
                                            conteudoProcessado = string.Format(numberFormat, "{0:0.00}", vDecimal);
                                            break;

                                        case TipoCampo.De3:
                                            conteudoProcessado = string.Format(numberFormat, "{0:0.000}", vDecimal);
                                            break;

                                        case TipoCampo.De4:
                                            conteudoProcessado = string.Format(numberFormat, "{0:0.0000}", vDecimal);
                                            break;

                                        case TipoCampo.De6:
                                            conteudoProcessado = string.Format(numberFormat, "{0:0.000000}", vDecimal);
                                            break;

                                        default:
                                            conteudoProcessado = string.Format(numberFormat, "{0:0.0000000000}", vDecimal);
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                estaVazio = true;
                            }

                            break;

                        case TipoCampo.Int:
                            int vInt;
                            if (int.TryParse(valor.ToString(), out vInt))
                            {
                                if (ocorrencia == Ocorrencia.MaiorQueZero && vInt == 0)
                                {
                                    estaVazio = true;
                                }
                                else
                                {
                                    conteudoProcessado = valor.ToString();
                                    if (conteudoProcessado.Length < min)
                                    {
                                        conteudoProcessado = conteudoProcessado.ZeroFill(min);
                                    }
                                }
                            }
                            else
                            {
                                estaVazio = true;
                            }
                            break;

                        case TipoCampo.StrNumberFill:
                            conteudoProcessado = valor.ToString();
                            if (conteudoProcessado.Length < min)
                            {
                                conteudoProcessado = conteudoProcessado.ZeroFill(min);
                            }
                            break;

                        case TipoCampo.StrNumber:
                            conteudoProcessado = valor.ToString().OnlyNumbers();
                            break;

                        default:
                            conteudoProcessado = valor.ToString();
                            break;
                    }
                }

                var alerta = string.Empty;
                if (ocorrencia == Ocorrencia.Obrigatoria && estaVazio)
                {
                    alerta = ErrMsgVazio;
                }

                if (!conteudoProcessado.IsEmpty() && conteudoProcessado.Length < min && alerta.IsEmpty() && conteudoProcessado.Length > 1)
                {
                    alerta = ErrMsgMenor;
                }

                if (!conteudoProcessado.IsEmpty() && conteudoProcessado.Length > max)
                {
                    alerta = ErrMsgMaior;
                }

                if (!alerta.IsEmpty() && ErrMsgVazio.Equals(alerta) && !estaVazio)
                {
                    alerta += $" [{valor}]";
                    WAlerta(id, tag, descricao, alerta);
                }

                XElement xmlTag = null;
                if (ocorrencia == Ocorrencia.Obrigatoria && estaVazio)
                {
                    xmlTag = GetElement(tag, string.Empty, ns);
                }

                return estaVazio ? xmlTag : GetElement(tag, conteudoProcessado, ns);
            }
            catch (Exception ex)
            {
                WAlerta(id, tag, descricao, ex.ToString());
                return GetElement(tag, string.Empty, ns);
            }
        }

        private static XElement GetElement(string name, string value, XNamespace ns = null)
        {
            return ns != null ? new XElement(ns + name, value) : new XElement(name, value);
        }

        /// <summary>
        /// Ws the alerta.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="descricao">The descricao.</param>
        /// <param name="alerta">The alerta.</param>
        protected void WAlerta(string id, string tag, string descricao, string alerta)
        {
            // O Formato da mensagem de erro pode ser alterado pelo usuario alterando-se a property FFormatoAlerta: onde;
            // %TAG%       : Representa a TAG; ex: <nLacre>
            // %ID%        : Representa a ID da TAG; ex X34
            // %MSG%       : Representa a mensagem de alerta
            // %DESCRICAO% : Representa a Descri��o da TAG

            var s = FormatoAlerta.Clone() as string;
            s = s.Replace("%ID%", id).Replace("%TAG%", $"<{tag}>")
                .Replace("%DESCRICAO%", descricao).Replace("%MSG%", alerta);

            ListaDeAlertas.Add(s);
            this.Log().Warn(s);
        }

        /// <summary>
        /// Valida o XML de acordo com o schema.
        /// </summary>
        /// <param name="xml">A mensagem XML que deve ser verificada.</param>
        /// <param name="schema">O schema que ser� usado na verifica��o.</param>
        /// <returns>Se estiver tudo OK retorna null, caso contr�rio as mensagens de alertas e erros.</returns>
        protected RetornoWebservice ValidarSchema(string xml, string schema)
        {
            schema = Path.Combine(Config.Geral.PathSchemas, Name, schema);
            string[] errosSchema;
            string[] alertasSchema;
            if (CertificadoDigital.ValidarXml(xml, schema, out errosSchema, out alertasSchema)) return null;

            var retLote = new RetornoWebservice
            {
                Assincrono = true,
                XmlEnvio = xml
            };

            foreach (var erro in errosSchema.Select(descricao => new Evento { Codigo = "0", Descricao = descricao }))
                retLote.Erros.Add(erro);

            foreach (var alerta in alertasSchema.Select(descricao => new Evento { Codigo = "0", Descricao = descricao }))
                retLote.Alertas.Add(alerta);

            return retLote;
        }

        /// <summary>
        /// Grava o xml da Rps no disco
        /// </summary>
        /// <param name="conteudoArquivo"></param>
        /// <param name="nomeArquivo"></param>
        /// <param name="data"></param>
        protected void GravarRpsEmDisco(string conteudoArquivo, string nomeArquivo, DateTime data)
        {
            if (Config.Arquivos.Salvar == false) return;

            GravarArquivoEmDisco(TipoArquivo.Rps, conteudoArquivo, nomeArquivo);
        }

        /// <summary>
        /// Grava o xml da NFSe no disco
        /// </summary>
        /// <param name="conteudoArquivo"></param>
        /// <param name="nomeArquivo"></param>
        /// <param name="data"></param>
        protected void GravarNFSeEmDisco(string conteudoArquivo, string nomeArquivo, DateTime data)
        {
            if (Config.Arquivos.Salvar == false) return;

            GravarArquivoEmDisco(TipoArquivo.NFSe, conteudoArquivo, nomeArquivo);
        }

        /// <summary>
        /// Grava o xml de comunica��o com o webservice no disco
        /// </summary>
        /// <param name="conteudoArquivo"></param>
        /// <param name="nomeArquivo"></param>
        protected void GravarArquivoEmDisco(string conteudoArquivo, string nomeArquivo)
        {
            if (Config.Geral.Salvar == false) return;

            GravarArquivoEmDisco(TipoArquivo.Webservice, conteudoArquivo, nomeArquivo);
        }

        private void GravarArquivoEmDisco(TipoArquivo tipo, string conteudoArquivo, string nomeArquivo, DateTime? data = null)
        {
            switch (tipo)
            {
                case TipoArquivo.Webservice:
                    nomeArquivo = Path.Combine(Config.Arquivos.GetPathLote(), nomeArquivo);
                    break;

                case TipoArquivo.Rps:
                    nomeArquivo = Path.Combine(Config.Arquivos.GetPathRps(data ?? DateTime.Today), nomeArquivo);
                    break;

                case TipoArquivo.NFSe:
                    nomeArquivo = Path.Combine(Config.Arquivos.GetPathNFSe(data ?? DateTime.Today), nomeArquivo);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null);
            }

            File.WriteAllText(nomeArquivo, conteudoArquivo, Encoding.UTF8);
        }

        #endregion Protected

        #region Dispose

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implement IDisposable Interface.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                //
            }

            certificado?.Reset();
            certificado = null;
            disposed = true;
        }

        #endregion Dispose

        #endregion Methods
    }
}