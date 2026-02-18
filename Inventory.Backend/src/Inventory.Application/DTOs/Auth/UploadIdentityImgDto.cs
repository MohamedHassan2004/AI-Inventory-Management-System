using Microsoft.AspNetCore.Http;

namespace Inventory.Application.DTOs.Auth
{
    public class UploadIdentityImgDto
    {
        public required IFormFile IdentityImageFile { get; set; }
    }
}