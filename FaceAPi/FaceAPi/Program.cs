using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceAPi
{
    static class Program
    {
        // **********************************************
        // *** Update or verify the following values. ***
        // **********************************************

        // Replace the subscriptionKey string value with your valid subscription key.
//        const string subscriptionKey = "bcb2327ef7764696aacd0c8c345ff55a";
        const string subscriptionKey = "7f91bde031eb43dd99befa9d28e8dea1";

        // Replace or verify the region.
        //
        // You must use the same region in your REST API call as you used to obtain your subscription keys.
        // For example, if you obtained your subscription keys from the westus region, replace 
        // "westcentralus" in the URI below with "westus".
        //
        // NOTE: Free trial subscription keys are generated in the westcentralus region, so if you are using
        // a free trial subscription key, you should not need to change this region.
//        const string uriBase = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/detect";
        const string uriBase = "https://northeurope.api.cognitive.microsoft.com/face/v1.0";


        static async Task Main()
        {

            // Create a person group
            var personGroupId = "criminal_take_6";
            
            
//            // Training Process
//            Console.WriteLine("Creating people group " + personGroupId);
//            await CreatePersonGroup(personGroupId, "Testing procedure five");
//            
//            // Need to grab the folder structure so we can grab individuals names from folder names
//            var folderRoute = "/Users/scottalexander/Development/MicrosftFaceApiTest-Console/Data/PersonGroupTest";
//            var peopleFolders = Directory.GetDirectories(folderRoute);
//
//            var i = 0;
//            // Lets add the people
//            foreach(var potentialPersonDir in peopleFolders)
//            {
//                i++;
//                var fileInfo = new FileInfo(potentialPersonDir);
//                var personName = fileInfo.Name;
//
//                var userData = "{'id': '" + i + "'}";
//                // Create person
//                var person = await CreatePerson(personGroupId, personName, userData);
//                await AddFacesToPerson(potentialPersonDir, personGroupId, person.PersonId);
//                Console.Write(personName + " " + "added");
//            }
//            
//            // Training time
//
//            TrainPersonGroup(personGroupId);
//            await WaitForPersonGroupTraining(personGroupId);
            
            // Identitify Process
            const string fileLocation = "/Users/scottalexander/Development/MicrosftFaceApiTest-Console/Data/test/IMG_6129.jpg";
            var x = await Identitify(fileLocation, personGroupId);

        }


        /// <summary>
        /// Gets the analysis of the specified image file by using the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file.</param>
        static async void MakeAnalysisRequest(string imageFilePath)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                // Display the JSON response.
                Console.WriteLine("\nResponse:\n");
                Console.WriteLine(JsonPrettyPrint(contentString));
            }
        }

        
        static async void FaceDetection(string imagePath)
        {
            
            var faceServiceClient = new FaceServiceClient(subscriptionKey, uriBase);
            
            var faces = await faceServiceClient.DetectAsync("https://previews.123rf.com/images/redbaron/redbaron0809/redbaron080900169/3608528-group-of-smiling-friends-looking-at-camera-closeup-on-faces-front-view-.jpg", true, true);

            foreach (var face in faces)
            {
                var rect = face.FaceRectangle;
                Console.WriteLine(rect);
                var landmarks = face.FaceLandmarks;
                Console.WriteLine(landmarks);
            }
            
        }

        static async Task<bool> CreatePersonGroup(string id, string description)
        {
            var faceServiceClient = new FaceServiceClient(subscriptionKey, uriBase);
            await faceServiceClient.CreatePersonGroupAsync(id, description);
            return true;
        }

        static async Task<CreatePersonResult> CreatePerson(string groupId, string name, string userData)
        {
            var faceServiceClient = new FaceServiceClient(subscriptionKey, uriBase);
            CreatePersonResult person = await faceServiceClient.CreatePersonAsync(groupId, name, userData);
            return person;

        }

        static async Task<bool> AddFacesToPerson(string imagesDirectory, string personGroupId, Guid personId)
        {
            
            var faceServiceClient = new FaceServiceClient(subscriptionKey, uriBase);

            var files = Directory.GetFiles(imagesDirectory, "*.JPG");
            foreach (var imagePath in files)
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    // Detect faces in the image and add to Anna
                    await faceServiceClient.AddPersonFaceAsync(personGroupId, personId, s);
                }
            }

            return true;
        }

        static async void IdentifyFacesAgainstPersonGroup(string personGroupId, string imageLocation)
        {
            var faceServiceClient = new FaceServiceClient(subscriptionKey, uriBase);
            string testImageFile = imageLocation;

            using (Stream s = File.OpenRead(testImageFile))
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Console.WriteLine("No one identified");
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                        Console.WriteLine("Identified as {0}", person.Name);
                    }
                }
            }
        }

        static async void TrainPersonGroup(string personGroupId)
        {
            var faceServiceClient = new FaceServiceClient(subscriptionKey, uriBase);
            await faceServiceClient.TrainPersonGroupAsync(personGroupId);
        }

        static async Task<bool> Identitify(string fileLocation, string personGroupId)
        {
            var faceServiceClient = new FaceServiceClient(subscriptionKey, uriBase);

            string testImageFile = fileLocation;

            using (Stream s = File.OpenRead(testImageFile))
            {

                var faces = await faceServiceClient.DetectAsync(s);
            

            var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Console.WriteLine("No one identified");
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                        Console.WriteLine("Identified as {0}", person.Name);
                    }
                }
                    
            }
            

            return true;
        }
        static async Task<bool> WaitForPersonGroupTraining(string personGroupId)
        {
            var faceServiceClient = new FaceServiceClient(subscriptionKey, uriBase);
            TrainingStatus trainingStatus = null;
            while(true)
            {
                trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                if (trainingStatus.Status != Microsoft.ProjectOxford.Face.Contract.Status.Running)
                {
                    return true;
                }

                await Task.Delay(10000);
            }    
        }
        
        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        

        /// <summary>
        /// Formats the given JSON string by adding line breaks and indents.
        /// </summary>
        /// <param name="json">The raw JSON string to format.</param>
        /// <returns>The formatted JSON string.</returns>
        static string JsonPrettyPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            json = json.Replace(Environment.NewLine, "").Replace("\t", "");

            StringBuilder sb = new StringBuilder();
            bool quote = false;
            bool ignore = false;
            int offset = 0;
            int indentLength = 3;

            foreach (char ch in json)
            {
                switch (ch)
                {
                    case '"':
                        if (!ignore) quote = !quote;
                        break;
                    case '\'':
                        if (quote) ignore = !ignore;
                        break;
                }

                if (quote)
                    sb.Append(ch);
                else
                {
                    switch (ch)
                    {
                        case '{':
                        case '[':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', ++offset * indentLength));
                            break;
                        case '}':
                        case ']':
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', --offset * indentLength));
                            sb.Append(ch);
                            break;
                        case ',':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', offset * indentLength));
                            break;
                        case ':':
                            sb.Append(ch);
                            sb.Append(' ');
                            break;
                        default:
                            if (ch != ' ') sb.Append(ch);
                            break;
                    }
                }
            }

            return sb.ToString().Trim();
        }
    }
}