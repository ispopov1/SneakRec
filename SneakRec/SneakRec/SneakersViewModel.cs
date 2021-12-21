using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace SneakRec
{
    public class SneakersViewModel : ViewModelBase
    {

        public SneakersViewModel()
        {
            TakePhotoCommand = new Command(async () => await TakePhoto());
            UploadPhotoCommand = new Command(async () => await UploadPhoto());
            ShowDetailsCommand = new Command(async () => await ShowDetails());
        }

        private async Task TakePhoto()
        {
            CanTakePhoto = false;
            await TakePhotoAndBuildMessage();
            CanTakePhoto = true;
        }
        private async Task UploadPhoto()
        {
            CanTakePhoto = false;
            await UploadPhotoAndBuildMessage();
            CanTakePhoto = true;
        }
        private async Task TakePhotoAndBuildMessage()
        {
            try
            {
                var options = new StoreCameraMediaOptions { PhotoSize = PhotoSize.Medium };
                var file = await CrossMedia.Current.TakePhotoAsync(options);
                var detailsMessage = string.Empty;
                NameMessage = BuildMessage(file, out detailsMessage);
                Details = detailsMessage;
                DeletePhoto(file);
            }
            catch (MediaPermissionException)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Check your camera permission", "OK");
            }
        }
        private async Task UploadPhotoAndBuildMessage()
        {
            try
            {
                var file = await CrossMedia.Current.PickPhotoAsync();
                var detailsMessage = string.Empty;
                NameMessage = BuildMessage(file, out detailsMessage);
                Details = detailsMessage;
            }
            catch (MediaPermissionException)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Check your storage permission", "OK");
            }
        }
        private async Task ShowDetails()
        {
            await Application.Current.MainPage.DisplayAlert("Details:", Details, "OK");
        }

        private string BuildMessage(MediaFile file, out string detailsMessage)
        {
            var message = "You need to photo sneakers";
            var details = string.Empty;
            try
            {
                if (file != null)
                {
                    var mostLikely = GetBestTag(file, out details);
                    if ((mostLikely == null) || (mostLikely.Probability < 0.55))
                    {
                        message = "I dont know what is it";
                        PredictionPhotoPath = "question.png";
                        CanShowDetails = true;
                    }
                    else
                    {
                        if (mostLikely.Probability < 0.75)
                        {
                            message = $"Probably it is {mostLikely.TagName}";
                            PredictionPhotoPath = mostLikely.TagName.Replace(' ', '_').Replace('-', '_') + ".jpg";
                            CanShowDetails = true;
                        }
                        else
                        {
                            message = $"It is {mostLikely.TagName}";
                            PredictionPhotoPath = mostLikely.TagName.Replace(' ', '_').Replace('-', '_') + ".jpg";
                            CanShowDetails = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            detailsMessage = details;
            return message;
        }

        private static void DeletePhoto(MediaFile file)
        {
            var path = file?.Path;

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                File.Delete(file?.Path);
        }

        private PredictionModel GetBestTag(MediaFile photo, out string details)
        {
            var endPoint = new CustomVisionPredictionClient
            {
                ApiKey = AppKeys.PredictionKey,
                Endpoint = AppKeys.EndPoint
            };
            var results = endPoint.ClassifyImage(Guid.Parse(AppKeys.ProjectId),
                                                          AppKeys.PublishName,
                                                          photo.GetStream());
            var predictions = results.Predictions
                                    .Where(p => p.Probability > 0.01)
                                    .ToList();

            var predictionDetails = string.Empty;
            foreach (var model in predictions)
            {
                predictionDetails += model.TagName + "  -  " + (model.Probability * 100).ToString("0.0")
                    + "%" + "\n";
            }

            details = predictionDetails;
            return predictions[0];
        }
        private string _predictionPhotoPath = "question.png";
        public string PredictionPhotoPath
        {
            get => _predictionPhotoPath;
            set => Set(ref _predictionPhotoPath, value);
        }

        private string _nameMessage = "You need to take photo";
        public string NameMessage
        {
            get => _nameMessage;
            set => Set(ref _nameMessage, value);
        }

        private bool _canTakePhoto = true;
        public bool CanTakePhoto
        {
            get => _canTakePhoto;
            set
            {
                if (Set(ref _canTakePhoto, value))
                    RaisePropertyChanged(nameof(ShowSpinner));
            }
        }

        private string _details = string.Empty;
        public string Details
        {
            get => _details;
            set
            {
                if (Set(ref _details, value))
                    RaisePropertyChanged(nameof(ShowSpinner));
            }
        }

        private bool _canShowDetails = false;
        public bool CanShowDetails
        {
            get => _canShowDetails;
            set
            {
                if (Set(ref _canShowDetails, value))
                    RaisePropertyChanged(nameof(ShowSpinner));
            }
        }
        public bool ShowSpinner => !CanTakePhoto;


        public ICommand TakePhotoCommand { get; }
        public ICommand UploadPhotoCommand { get; }
        public ICommand ShowDetailsCommand { get; }

    }
}
