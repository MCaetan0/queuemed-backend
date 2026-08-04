using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QRCoder;
using QueueMed.Application.Options;

namespace QueueMed.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly QrCodeOptions _options;

    public AdminController(IOptions<QrCodeOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("qrcode")]
    [Produces("image/png")]
    public IActionResult GetQrCode()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(_options.EntryUrl, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(10);
        return File(bytes, "image/png");
    }
}
