using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using static System.IO.Directory;

namespace FaceAPi
{
    static class Program
    {
        // **********************************************
        // *** Update or verify the following values. ***
        // **********************************************

        // Replace the subscriptionKey string value with your valid subscription key.
        const string SubscriptionKey = "7f91bde031eb43dd99befa9d28e8dea1";

        // Replace or verify the region.
        const string UriBase = "https://northeurope.api.cognitive.microsoft.com/face/v1.0";
        private static readonly string RootFolder = "/Users/scottalexander/Development/MicrosftFaceApiTest-Console";
        private static readonly string TrainingFolderPath = RootFolder + "/Data/PersonGroupTraining";


        static async Task Main()
        {
            // define it once and pass it through
            var faceServiceClient = new  FaceServiceClient(SubscriptionKey, UriBase);
            
            // personGroupId
            //var personGroupId = "criminal_take_6";
            var personGroupId = "";
            var menuInput = "";

            while (menuInput == "")
            {
                Console.WriteLine("Welcome to the Azure Face API trainer, input the menu option from below to start...");
                Console.WriteLine("1: Set a Person Group");
                Console.WriteLine("2: Create and set a Person Group");

                if (!string.IsNullOrEmpty(personGroupId))
                {
                    Console.WriteLine("3: Train faces from folder structure (more info on selection)");
                    Console.WriteLine("4: Identitfy face");
                }
                else
                {
                    Console.WriteLine("-: More options will become avaliable once a Person Group has been set or created");
                }

                Console.WriteLine("5: Exit application");
                
                menuInput = Console.ReadLine();
                Console.Clear();
                
                if (!int.TryParse(menuInput, out var numericalInput))
                {
                    Console.WriteLine("Your option has not been recognised, please try again (1-5)");
                    menuInput = "";
                }
                else
                {

                    // case 5 will just continue without setting menuInput back to ""
                    switch (numericalInput)
                    {
                        case 1:
                        {
                            var inputGroup = "";
                            // Set a person group
                            while (inputGroup == "")
                            {
                                Console.WriteLine(
                                    "Please input the name of your expected Person Group (we will check it exists)");
                                inputGroup = Console.ReadLine();
                            }

                            Console.Clear();
                            var res = await PersonGroupExists(faceServiceClient, inputGroup);
                            if (res.Contains("Error"))
                            {
                                Console.WriteLine(res);
                                Console.WriteLine("Press any key to continue...");
                                Console.ReadLine();
                                Console.Clear();
                            }
                            else
                            {
                                Console.WriteLine("Person Group " + res + " has been found and set");
                                Console.WriteLine("      ");
                                personGroupId = res;
                            }

                            // restart from the start
                            menuInput = "";
                            break;
                        }
                        case 2:
                        {

                            var nameInput = "";
                            // Create a person group

                            while (nameInput == "")
                            {
                                Console.WriteLine("Please input a valid Name / Id for your Person Group");
                                nameInput = Console.ReadLine();
                            }

                            Console.Clear();

                            var res = await CreatePersonGroup(faceServiceClient, nameInput, "Creating " + nameInput);
                            if (res.Contains("Error"))
                            {
                                Console.WriteLine(res);
                                Console.WriteLine("Press any key to continue...");
                                Console.ReadLine();
                                Console.Clear();
                            }
                            else
                            {
                                Console.WriteLine("Person Group with id of " + nameInput + " has been created successfully");
                                Console.WriteLine("     ");
                                personGroupId = nameInput;
                            }

                            // restart from the start
                            menuInput = "";
                            break;
                        }
                        case 3:
                        {
                            // Train faces
                            
                            Console.WriteLine("By default this application will look for folders in applicationroot/Data/PersonGroupTraining");
                            Console.WriteLine("There are currently test images there but you can replace them with a folder structure where the folder is the individuals name and inside are single faced images of the individual in .JPG format. ");
                            Console.WriteLine("    ");
                            Console.WriteLine("Are you currently using a free subscription? (y/n)");
                            var freeSubscription = "";
                            while (freeSubscription == "")
                            {
                                freeSubscription = Console.ReadLine();
                                freeSubscription = freeSubscription?.ToLower();
                                if (!(freeSubscription == "y" || freeSubscription == "n"))
                                {
                                    freeSubscription = "";
                                }
                                else
                                {
                                    Console.WriteLine("Please answer with y or n ");
                                }
                            }
                            
                            Console.Clear();

                            if (freeSubscription == "y")
                            {
                                Console.WriteLine("Free subscriptions can only make 20 API calls per minute, therefore, the training process will be slowed down for you");
                            }
                            
                            Console.WriteLine("When you are ready to train your images press return");
                            Console.ReadLine();
                            Console.Clear();

                            var res = await Train(faceServiceClient, personGroupId, freeSubscription == "y");
                            Console.WriteLine("Training completed");
                            Console.WriteLine("      ");
                            
                            // restart from the start
                            menuInput = "";
                            break;
                        }
                        case 4:
                        {
                            // Identifiy a face
                            Console.WriteLine("Please provide the file path of the .JPG image that you would like to identify faces in");

                            var testImageLocation = "";
                            while (testImageLocation == "")
                            {
                                testImageLocation = Console.ReadLine();
                                if (!File.Exists(testImageLocation))
                                {
                                    testImageLocation = "";
                                    Console.WriteLine("The file you have specified does not exist");
                                }

                            }
                            
                            var res = await Identitify(faceServiceClient, testImageLocation, personGroupId);
                            // restart from the start
                            
                            Console.WriteLine(res);
                            Console.WriteLine("    ");
                            menuInput = "";
                            break;
                        }
                    }       
                }
            }
        }

        static async Task<string> PersonGroupExists(FaceServiceClient faceServiceClient, string personGroupId)
        {
            try
            {
                var group = await faceServiceClient.GetPersonGroupAsync(personGroupId);
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }

            return personGroupId;
        }
       
        static async Task<string> CreatePersonGroup(FaceServiceClient faceServiceClient, string id, string description)
        {
            try
            {
                await faceServiceClient.CreatePersonGroupAsync(id, description);
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }

            return id;
        }

        static async Task<string> Train(FaceServiceClient faceServiceClient, string personGroupId, bool limit = false)
        {
            var peopleFolders = GetDirectories(TrainingFolderPath);
            
//           Lets add the people
            foreach(var potentialPersonDir in peopleFolders)
            {

                var fileInfo = new FileInfo(potentialPersonDir);
                var personName = fileInfo.Name;

                // Add any additional data you'd like assigned to your person here
                //var userData = //JSON STRING
                
                // Create person (we can't check to see if the person exists already as we don't have there unique identification number at this point)
                var person = await CreatePerson(faceServiceClient, personGroupId, personName, null); // userData is the last parameter
                if (person != null)
                {
                    await AddFacesToPerson(faceServiceClient, potentialPersonDir, personGroupId, person.PersonId, limit);
                    Console.WriteLine(personName + " " + "added successfully");
                }

                if (limit)
                {
                    System.Threading.Thread.Sleep(15000);
                }
            }
            
            // If we have not crashed out here then it's time to train the images
            TrainPersonGroup(faceServiceClient, personGroupId);
            await WaitForPersonGroupTraining(faceServiceClient, personGroupId, limit);

            return "Completed";
        }
        
        static async Task<CreatePersonResult> CreatePerson(FaceServiceClient faceServiceClient, string groupId, string name,
            string userData)
        {
            try
            {
                CreatePersonResult person = await faceServiceClient.CreatePersonAsync(groupId, name, userData);
                return person;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create person " + name);
                Console.WriteLine("Error: " + ex.Message);
            }

            return null;
        }
        
        static async Task<bool> AddFacesToPerson(FaceServiceClient faceServiceClient, string imagesDirectory,
            string personGroupId, Guid personId, bool limit = false)
        {
            var files = GetFiles(imagesDirectory, "*.JPG").Union(GetFiles(imagesDirectory, "*.jpg")).ToArray();;
            foreach (var imagePath in files)
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    // Detect faces in the image and add to person
                    try
                    {
                        await faceServiceClient.AddPersonFaceAsync(personGroupId, personId, s);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error has occured with image " + imagePath + " and has not been added to person");
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }

                if (limit)
                {
                    System.Threading.Thread.Sleep(15000);
                }
            }

            return true;
        }
        
        static async void TrainPersonGroup(FaceServiceClient faceServiceClient, string personGroupId)
        {
            await faceServiceClient.TrainPersonGroupAsync(personGroupId);
        }
        
        static async Task<bool> WaitForPersonGroupTraining(FaceServiceClient faceServiceClient, string personGroupId,
            bool limit = false)
        {
            while(true)
            {
                var trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                if (trainingStatus.Status != Status.Running)
                {
                    return true;
                }

                if (limit)
                {
                    System.Threading.Thread.Sleep(15000);
                }
            }    
        }
        
        static async Task<string> Identitify(FaceServiceClient faceServiceClient, string fileLocation, string personGroupId)
        {

            string testImageFile = fileLocation;

            using (Stream s = File.OpenRead(testImageFile))
            {

                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Console.WriteLine("No one identified");
                    }
                    else
                    {
                        foreach (var candidate in identifyResult.Candidates)
                        {
                            var candidateId = candidate.PersonId;
                            try
                            {
                                var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                                Console.WriteLine("     ");
                                Console.WriteLine("Identified as {0}", person.Name);
                            }
                            catch
                            {
                                Console.WriteLine("      ");
                                Console.WriteLine("Unable to retrieve Person data for ID " + candidateId);
                            }  
                        }
                    }
                }

            }

            return "Identification process complete";
        }
        
    }
}