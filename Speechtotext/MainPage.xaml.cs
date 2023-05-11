using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Azure.AI.OpenAI;
using Azure;
using Windows.Media.SpeechRecognition;
using System.Diagnostics;
using Windows.Media.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Newtonsoft.Json;
using System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Speechtotext
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"
        static string speechKey = "";
        static string speechRegion = "";
        Microsoft.CognitiveServices.Speech.SpeechRecognizer speechRecognizer;

        static string openAIKey = "";
        static string openAIEndpoint = "";

        // Enter the deployment name you chose when you deployed the model.
        static string engine = "";

        public class Provider
        {
            public string type { get; set; }
            public string voice_id { get; set; }
        }

        public class Root
        {
            public Script script { get; set; }
            public string source_url { get; set; }
        }

        public class Script
        {
            public string type { get; set; }
            public string input { get; set; }
            public Provider provider { get; set; }
        }

        public class RootVideo
        {
            public object user { get; set; }
            public object metadata { get; set; }
            public string audio_url { get; set; }
            public DateTime created_at { get; set; }
            public object face { get; set; }
            public object config { get; set; }
            public string source_url { get; set; }
            public string created_by { get; set; }
            public string status { get; set; }
            public string driver_url { get; set; }
            public DateTime modified_at { get; set; }
            public string user_id { get; set; }
            public string result_url { get; set; }
            public string id { get; set; }
            public int duration { get; set; }
            public DateTime started_at { get; set; }
        }


        public class RootVideoIdReq
        {
            public string id { get; set; }
            public DateTime created_at { get; set; }
            public string created_by { get; set; }
            public string status { get; set; }
        }

        private const string ApiUri = "https://api.d-id.com/talks";

        private string currentVideoLink = "ms-appx:///Assets/sampleMockVid.mp4";

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                if (Dispatcher == null) // For console App
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    if (Dispatcher.HasThreadAccess)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    else
                    {
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => PropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
                    }
                }
            }
        }

        public MainPage()
        {
            InitializeComponent();

            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = "en-IN";

            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            speechRecognizer = new Microsoft.CognitiveServices.Speech.SpeechRecognizer(speechConfig, audioConfig);

            //string videSrc = Task.Run(async () => await ConvertTextToVideoLinkId("Hello, this is a demo for our hackathon 2023 project on Open AI.This is an AI generated video.")).Result;
            string videSrc = "https://d-id-talks-prod.s3.us-west-2.amazonaws.com/google-oauth2%7C115582271522269271554/tlk_QgfrvTxfb6nGhGNSt-l6H/1683622090499.mp4?AWSAccessKeyId=AKIA5CUMPJBIK65W6FGA&Expires=1683708495&Signature=PayJhhr%2FrRA0FiJMFgatiVZn%2B54%3D&X-Amzn-Trace-Id=Root%3D1-645a08cf-5de44550482bcf775dcfe6a3%3BParent%3D69bf43941cbf7c34%3BSampled%3D1%3BLineage%3D6b931dd4%3A0";

            this.currentVideoLink = videSrc;
            VideoState.Source = MediaSource.CreateFromUri(new Uri(videSrc));
            VideoState.Visibility = Visibility.Visible;
        }


        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();

            switch (speechRecognitionResult.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                    await AskOpenAI(speechRecognitionResult.Text).ConfigureAwait(true);
                    break;
                case ResultReason.NoMatch:
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    break;
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }
                    break;
            }
        }

        async Task AskOpenAI(string prompt)
        {
            // Ask Azure OpenAI
            OpenAIClient client = new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));
            var completionsOptions = new CompletionsOptions()
            {
                Prompts = { prompt },
                MaxTokens = 100,
            };
            Response<Completions> completionsResponse = client.GetCompletions(engine, completionsOptions);
            string text = completionsResponse.Value.Choices[0].Text.Trim();
            Console.WriteLine($"Azure OpenAI response: {text}");

            string videSrc = Task.Run(async () => await ConvertTextToVideoLinkId(text)).Result;

            this.currentVideoLink = videSrc;
            VideoState.Source = MediaSource.CreateFromUri(new Uri(videSrc));
            VideoState.Visibility = Visibility.Visible;
            //VideoState.AutoPlay = true;


            /*var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            // The language of the voice that speaks.
            speechConfig.SpeechSynthesisVoiceName = "en-US-JennyMultilingualNeural";
            var audioOutputConfig = AudioConfig.FromDefaultSpeakerOutput();

            using (var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioOutputConfig))
            {
                var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(text).ConfigureAwait(true);

                if (speechSynthesisResult.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    Console.WriteLine($"Speech synthesized to speaker for text: [{text}]");
                }
                else if (speechSynthesisResult.Reason == ResultReason.Canceled)
                {
                    var cancellationDetails = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                    Console.WriteLine($"Speech synthesis canceled: {cancellationDetails.Reason}");

                    if (cancellationDetails.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"Error details: {cancellationDetails.ErrorDetails}");
                    }
                }
            }*/
        }

        public string CurrentVideoLink { get { return this.currentVideoLink; } set { this.currentVideoLink = value; OnPropertyChanged(); } }


        private static async Task<string> FetchTheVideoLink(string id)
        {
            string respVideoSrc = "ms-appx:///Assets/sampleMockVid.mp4";
            try
            {
                Debug.WriteLine("[IdToVideoLink] Fetchingthe video url started: ");
                string requestUri = ApiUri + "/" + id;
                Uri VideoReqUrl = new Uri(requestUri);

                HttpClient client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, VideoReqUrl);
                request.Headers.Add("Authorization", "Basic WkdWbGNHRnJMblJwZDJGeWFTNHhPVGc1UUdkdFlXbHNMbU52YlE6d0lCTTJ0ejhTZTJITTRtTVJUdkVN");
                var content = new StringContent("", null, "text/plain");
                request.Content = content;
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var resp = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("[IdToVideoLink] Fetchingthe video url response: " + resp);
                RootVideo videoResp = JsonConvert.DeserializeObject<RootVideo>(resp);
                Debug.WriteLine($"[IdToVideoLink] Fetchingthe video url from object:  {videoResp.result_url}");


                respVideoSrc = videoResp.result_url;
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"[IdToVideoLink] Fetchingthe video url sexceptiond: {ex.Message}");
            }

            return respVideoSrc;

        }

        private static async Task<string> ConvertTextToVideoLinkId(string text)
        {
            string respVideoSrc = "ms-appx:///Assets/sampleMockVid.mp4";
            try
            {
                Debug.WriteLine("[ConvertTextToVideoLinkId] Fetching the video from text started: ");
                Uri VideoReqUrl = new Uri(ApiUri);
                HttpClient client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.d-id.com/talks");
                request.Headers.Add("Authorization", "Basic WkdWbGNHRnJMblJwZDJGeWFTNHhPVGc1UUdkdFlXbHNMbU52YlE6d0lCTTJ0ejhTZTJITTRtTVJUdkVN");

                Script scriptC = new Script();
                scriptC.type = "text";
                scriptC.input = text;
                Provider pr = new Provider();
                pr.type = "microsoft";
                pr.voice_id = "en-US-JennyNeural";
                scriptC.provider = pr;
                Root root = new Root();
                root.script = scriptC;
                root.source_url = "https://s-media-cache-ak0.pinimg.com/736x/b2/b5/b3/b2b5b30e4b4b75365ce57ca30040e76a--female-faces-female-face-claims.jpg";

                string contS = JsonConvert.SerializeObject(root);
                var content = new StringContent(contS, null, "application/json");
                request.Content = content;
                Debug.WriteLine("[ConvertTextToVideoLinkId] Fetching the video result2 respest: " + contS);
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var resp = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("[ConvertTextToVideoLinkId] Fetchingthe video result2 response: " + resp);
                RootVideoIdReq respVideoId = JsonConvert.DeserializeObject<RootVideoIdReq>(resp);
                Debug.WriteLine($"[ConvertTextToVideoLinkId] Fetching the video url from object:  {respVideoId.id}");

                Thread.Sleep(5000);
                respVideoSrc = await FetchTheVideoLink(respVideoId.id).ConfigureAwait(false);
                return respVideoSrc;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConvertTextToVideoLinkId] Fetching the video text sexceptiond: {ex.Message}");
            }

            return respVideoSrc;
        }


        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {

            StatusTextBlock.Text = "Recording stopped...";
            // SpeechRecognizer_ResultGenerated();
        }

        /*private async void SpeechRecognizer_ResultGenerated()
        {
            try
            {
                var audioFile = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync("recording.wav");
                var audioStream = await audioFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

                var url = "https://api.openai.com/v1/speech-to-text";

                var content = new MultipartFormDataContent();
                var audioContent = new StreamContent(audioStream.AsStreamForRead());
                audioContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                {
                    Name = "file",
                    FileName = "recording.wav"
                };
                content.Add(audioContent);

                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var transcriptionUrl = "https://api.openai.com/v1/engines/text-davinci-002/completions";
                var transcriptionRequest = new
                {
                    prompt = "Transcribe the following audio:\n\n" + responseContent,
                    max_tokens = 1284,
                    temperature = 0.7,
                    n = 1,
                    stop = "\n"
                };
                var transcriptionContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(transcriptionRequest));
                transcriptionContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var transcriptionResponse = await httpClient.PostAsync(transcriptionUrl, transcriptionContent);
                var transcriptionResponseContent = await transcriptionResponse.Content.ReadAsStringAsync();

            }
            catch { 
                string x = "hi";
            }
        }*/
    }
}