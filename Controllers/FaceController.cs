using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EZ2RMBBackendCore.Controllers
{
    public class FaceController : Controller
    {
        const string subscriptionKey = "2f3f09fbd5c24c54b001691f2a123e6a";
        const string uriBase = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0";

        [HttpPost("/api/Face")]
        public async Task<FaceVerifyModel> Post([FromBody]FacePhotoModel photoModel)
        {
            //var faceId1 = await GetFaceIdFromPhoto(photoModel.File1Base64);
            //var faceId2 = await GetFaceIdFromPhoto(photoModel.File2Base64);

            var faceId1 = GetFaceIdFromPhoto(photoModel.File1Base64).Result;
            var faceId2 = GetFaceIdFromPhoto(photoModel.File2Base64).Result;

            var result = await GetScoreOfFaces(faceId1, faceId2);
            return result;
        }

        static byte[] GetImageAsByteArray(string photoBase64)
        {
            return Convert.FromBase64String(photoBase64);
        }

        static async Task<string> GetFaceIdFromPhoto(string photoBase64)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            string requestParameters = "returnFaceId=true";
            string uri = uriBase + "/detect" + "?" + requestParameters;

            HttpResponseMessage response;
            byte[] byteData = GetImageAsByteArray(photoBase64);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                //response = await client.PostAsync(uri, content);
                response = client.PostAsync(uri, content).Result;
                string contentString = await response.Content.ReadAsStringAsync();
                var resultArr = JsonConvert.DeserializeObject<List<FaceDetectModel>>(contentString);
                return resultArr[0].FaceId;
            }
        }

        static async Task<FaceVerifyModel> GetScoreOfFaces(string faceId1, string faceId2)
        {
            string uri = uriBase + "/verify";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var response = await client.PostAsJsonAsync(
                uri, new FaceVerifyModelRequest { FaceId1 = faceId1, FaceId2 = faceId2 });

            var result = JsonConvert.DeserializeObject<FaceVerifyModel>(await response.Content.ReadAsStringAsync());
            return result;
        }
    }

    public class FacePhotoModel
    {
        public string File1Base64 { get; set; }
        public string File2Base64 { get; set; }
    }

    public class FaceDetectModel
    {
        public string FaceId { get; set; }
    }

    public class FaceVerifyModel
    {
        public bool IsIdentical { get; set; }
        public double Confidence { get; set; }
    }

    public class FaceVerifyModelRequest
    {
        public string FaceId1 { get; set; }
        public string FaceId2 { get; set; }
    }

    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(
            this HttpClient httpClient, string url, T data)
        {
            var dataAsString = JsonConvert.SerializeObject(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PostAsync(url, content);
        }

        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content)
        {
            var dataAsString = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(dataAsString);
        }
    }
}
