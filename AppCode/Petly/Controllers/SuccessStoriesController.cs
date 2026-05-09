using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Petly.Business.Services;
using Petly.Models;
using Microsoft.AspNetCore.Hosting; 
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;


namespace Petly.Controllers;

public class SuccessStoriesController : Controller
{
    private readonly SuccessStoryService _storyService;
    private readonly IConfiguration _config;

    public SuccessStoriesController(SuccessStoryService storyService, IConfiguration config)
    {
        _storyService = storyService;
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var stories = await _storyService.GetAllStoriesAsync();
        return View(stories);
    }

    [Authorize(Roles = "shelter_admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Pets = await _storyService.GetAvailablePetsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "shelter_admin")]
    public async Task<IActionResult> Create(SuccessStory story, IFormFile? uploadFile)
    {
        ModelState.Remove("Pet"); 

        if (ModelState.IsValid)
        {
            if (uploadFile != null && uploadFile.Length > 0)
            {
                var account = new Account(
                _config["CloudinarySettings:CloudName"],
                _config["CloudinarySettings:ApiKey"],
                _config["CloudinarySettings:ApiSecret"]
                );
                var cloudinary = new Cloudinary(account);
                using (var stream = uploadFile.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(uploadFile.FileName, stream),
                        Folder = "petly_success_stories" 
                    };
                    var uploadResult = await cloudinary.UploadAsync(uploadParams);
                    story.ImageUrl = uploadResult.SecureUrl.AbsoluteUri;
                }
            }
            await _storyService.CreateStoryAsync(story);
            return RedirectToAction(nameof(Index));
        }
        
        ViewBag.Pets = await _storyService.GetAvailablePetsAsync();
        return View(story);
    }

    [Authorize(Roles = "shelter_admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var story = await _storyService.GetStoryByIdAsync(id);
        if (story == null) return NotFound();

        ViewBag.Pets = await _storyService.GetAvailablePetsAsync();
        return View(story);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "shelter_admin")]
    public async Task<IActionResult> Edit(int id, SuccessStory story, IFormFile? uploadFile)
    {
        if (id != story.Id) return BadRequest();

        ModelState.Remove("Pet");

        if (ModelState.IsValid)
        {
            if (uploadFile != null && uploadFile.Length > 0)
            {
                var account = new Account(
                _config["CloudinarySettings:CloudName"],
                _config["CloudinarySettings:ApiKey"],
                _config["CloudinarySettings:ApiSecret"]
                );
                var cloudinary = new Cloudinary(account);
                using (var stream = uploadFile.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(uploadFile.FileName, stream),
                        Folder = "petly_success_stories" 
                    };
                    var uploadResult = await cloudinary.UploadAsync(uploadParams);
                    story.ImageUrl = uploadResult.SecureUrl.AbsoluteUri;
                }
            }

            await _storyService.UpdateStoryAsync(story);
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Pets = await _storyService.GetAvailablePetsAsync();
        return View(story);
    }
}