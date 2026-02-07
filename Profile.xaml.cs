using Microsoft.Maui.Media;
using System.IO;

namespace ImanGrow
{
    public partial class Profile : ContentPage
    {
        public Profile()
        {
            InitializeComponent();
        }

        // ==============================
        //   CHANGE PHOTO (ACTION SHEET)
        // ==============================
        private async void OnChangePhotoTapped(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet(
                "Change Profile Photo",
                "Cancel",
                null,
                "Choose from Gallery",
                "Take a Photo");

            if (action == "Choose from Gallery")
                await PickImageFromGallery();

            if (action == "Take a Photo")
                await CapturePhoto();
        }

        // ==============================
        //      SELECT FROM GALLERY
        // ==============================
        private async Task PickImageFromGallery()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images,
                    PickerTitle = "Select Profile Picture"
                });

                if (result == null)
                    return;

                using var stream = await result.OpenReadAsync();
                var memory = new MemoryStream();
                await stream.CopyToAsync(memory);
                memory.Position = 0;

                // IMPORTANT: Always use NEW STREAM
                ProfileImage.Source = ImageSource.FromStream(() =>
                    new MemoryStream(memory.ToArray()));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to pick image:\n{ex.Message}", "OK");
            }
        }

        // ==============================
        //          CAPTURE PHOTO
        // ==============================
        private async Task CapturePhoto()
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlert("Error", "Camera not available.", "OK");
                    return;
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null)
                    return;

                using var stream = await photo.OpenReadAsync();
                var memory = new MemoryStream();
                await stream.CopyToAsync(memory);
                memory.Position = 0;

                // Always assign a NEW stream
                ProfileImage.Source = ImageSource.FromStream(() =>
                    new MemoryStream(memory.ToArray()));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to capture photo:\n{ex.Message}", "OK");
            }
        }
        private async void OnAboutTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new About());
        }

    }
}
