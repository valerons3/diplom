namespace NeironBackend.Services.Interfaces
{
    public interface IFileService
    {
        public Task<(bool Success, string? Message, byte[]? fileBytes)> UploadFile(string userID, string processID, string fileName);
        public Task DeleteUserFolderAsync(string userId);
    }
}
