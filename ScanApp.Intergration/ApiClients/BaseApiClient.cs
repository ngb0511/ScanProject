﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ScanApp.Intergration.ApiClients
{
    public class BaseApiClient
    {
        //private readonly string _apiAddress = "http://192.168.1.15:4003/";
        //private readonly string _apiAddress = "http://idcag.librasoft.vn/";
        private readonly string _apiAddress;

        protected BaseApiClient()
        {
            //_apiAddress = ReadApiAddress();
        }

        private string ReadApiAddress()
        {
            //string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apiAddress.txt");
            string filePath = "D:\\GitLab\\OriScan\\OriginalScan\\apiAddress.txt";

            string apiAddress = "";

            try
            {
                if (File.Exists(filePath))
                {
                    apiAddress = File.ReadAllText(filePath);
                }
                else
                {
                    throw new Exception($"Tập tin không tồn tại!");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi đường dẫn: {ex.Message}");
            }

            return apiAddress;
        }

        protected async Task<bool> AddFileAsync(string url, IFormFile file)
        {
            try
            {
                if (file.Length > 0)
                {
                    using var client = new HttpClient();
                    client.BaseAddress = new Uri(_apiAddress);

                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    using var form = new MultipartFormDataContent();
                    using var fileContent = new StreamContent(memoryStream);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    form.Add(fileContent, "pdfs", file.FileName);
                    HttpResponseMessage response = await client.PostAsync(url, form);
                    return response.IsSuccessStatusCode;
                }
                return false;
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException($"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred during processing: " + ex.Message);
            }
        }

    }
}
