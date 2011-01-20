/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using DotCMIS.Binding.Impl;
using DotCMIS.Enums;
using DotCMIS.Exceptions;
using System.Web;
using System.Text;

namespace DotCMIS.Binding
{
    internal static class HttpUtils
    {
        public delegate void Output(Stream stream);

        public static Response InvokeGET(UrlBuilder url, BindingSession session)
        {
            return Invoke(url, "GET", null, null, session, null, null);
        }

        public static Response InvokeGET(UrlBuilder url, BindingSession session, int? offset, int? length)
        {
            return Invoke(url, "GET", null, null, session, offset, length);
        }

        public static Response InvokePOST(UrlBuilder url, String contentType, Output writer, BindingSession session)
        {
            return Invoke(url, "POST", contentType, writer, session, null, null);
        }

        public static Response InvokePUT(UrlBuilder url, String contentType, Output writer, BindingSession session)
        {
            return Invoke(url, "PUT", contentType, writer, session, null, null);
        }

        public static Response InvokeDELETE(UrlBuilder url, BindingSession session)
        {
            return Invoke(url, "DELETE", null, null, session, null, null);
        }

        private static Response Invoke(UrlBuilder url, String method, String contentType, Output writer, BindingSession session,
                int? offset, int? length)
        {
            try
            {
                // log before connect
                Trace.WriteLine(method + " " + url);

                // create connection           
                HttpWebRequest conn = (HttpWebRequest)WebRequest.Create(url.Url);
                conn.Method = method;

                // set content type
                if (contentType != null)
                {
                    conn.ContentType = contentType;
                }

                // authenticate
                AbstractAuthenticationProvider authProvider = session.GetAuthenticationProvider();
                if (authProvider != null)
                {
                    conn.PreAuthenticate = true;
                    authProvider.Authenticate(conn);
                }

                // range
                if (offset != null && length != null)
                {
                    conn.AddRange(offset ?? 0, offset + length - 1 ?? 0);
                }
                else if (offset != null)
                {
                    conn.AddRange(offset ?? 0);
                }

                // send data
                if (writer != null)
                {
                    conn.SendChunked = true;
                    conn.AllowWriteStreamBuffering = false;
                    Stream requestStream = conn.GetRequestStream();
                    writer(requestStream);
                    requestStream.Close();
                }

                // connect
                try
                {
                    HttpWebResponse response = (HttpWebResponse)conn.GetResponse();
                    return new Response(response);
                }
                catch (WebException we)
                {
                    return new Response(we);
                }
            }
            catch (Exception e)
            {
                throw new CmisConnectionException("Cannot access " + url + ": " + e.Message, e);
            }
        }

        internal class Response
        {
            private WebResponse response;

            public HttpStatusCode StatusCode { get; private set; }
            public string Message { get; private set; }
            public Stream Stream { get; private set; }
            public string ErrorContent { get; private set; }
            public string ContentType { get; private set; }
            public long? ContentLength { get; private set; }

            public Response(HttpWebResponse httpResponse)
            {
                this.response = httpResponse;
                StatusCode = httpResponse.StatusCode;
                Message = httpResponse.StatusDescription;
                ContentType = httpResponse.ContentType;
                ContentLength = httpResponse.ContentLength == -1 ? null : (long?)httpResponse.ContentLength;

                if (httpResponse.StatusCode == HttpStatusCode.OK ||
                    httpResponse.StatusCode == HttpStatusCode.Created ||
                    httpResponse.StatusCode == HttpStatusCode.NonAuthoritativeInformation ||
                    httpResponse.StatusCode == HttpStatusCode.PartialContent)
                {
                    Stream = new BufferedStream(httpResponse.GetResponseStream(), 64 * 1024);
                }
                else
                {
                    try { httpResponse.Close(); }
                    catch (Exception) { }
                }
            }

            public Response(WebException exception)
            {
                response = exception.Response;

                if (response is HttpWebResponse)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    StatusCode = httpResponse.StatusCode;
                    Message = httpResponse.StatusDescription;
                    ContentType = httpResponse.ContentType;

                    if (ContentType != null && ContentType.ToLower().StartsWith("text/"))
                    {
                        StringBuilder sb = new StringBuilder();

                        using (StreamReader sr = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            string s;
                            while ((s = sr.ReadLine()) != null)
                            {
                                sb.Append(s);
                                sb.Append('\n');
                            }
                        }

                        ErrorContent = sb.ToString();
                    }
                }
                else
                {
                    StatusCode = HttpStatusCode.InternalServerError;
                    Message = exception.Status.ToString();
                }

                try { response.Close(); }
                catch (Exception) { }
            }
        }
    }

    internal class UrlBuilder
    {
        private UriBuilder uri;

        public Uri Url
        {
            get { return uri.Uri; }
        }

        public UrlBuilder(string url)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            uri = new UriBuilder(url);
        }

        public UrlBuilder AddParameter(string name, object value)
        {
            if ((name == null) || (value == null))
            {
                return this;
            }

            string valueStr = HttpUtility.UrlEncode(NormalizeParameter(value));

            if (uri.Query != null && uri.Query.Length > 1)
            {
                uri.Query = uri.Query.Substring(1) + "&" + name + "=" + valueStr;
            }
            else
            {
                uri.Query = name + "=" + valueStr;
            }

            return this;
        }

        public static string NormalizeParameter(object value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is Enum)
            {
                return ((Enum)value).GetCmisValue();
            }
            else if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }

            return value.ToString();
        }

        public override string ToString()
        {
            return Url.ToString();
        }
    }
}
