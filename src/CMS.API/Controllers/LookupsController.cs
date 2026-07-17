using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupRepository _repository;

    public LookupsController(ILookupRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Slim AppUser list for the role-users multiselect.</summary>
    [HttpGet("app-users")]
    public async Task<ActionResult<IEnumerable<AppUserLookup>>> GetAppUsers()
    {
        var users = await _repository.GetAppUsersAsync();
        return Ok(users);
    }

    /// <summary>Slim AppRole list for the user-roles multiselect and role filter.</summary>
    [HttpGet("app-roles")]
    public async Task<ActionResult<IEnumerable<AppRoleLookup>>> GetAppRoles()
    {
        var roles = await _repository.GetAppRolesAsync();
        return Ok(roles);
    }

    /// <summary>Slim PublishStatus list for FK dropdowns (Course, Promotion2).</summary>
    [HttpGet("publish-statuses")]
    public async Task<ActionResult<IEnumerable<PublishStatusLookup>>> GetPublishStatuses()
    {
        var statuses = await _repository.GetPublishStatusesAsync();
        return Ok(statuses);
    }

    /// <summary>Slim Partner list for FK dropdowns (Course, Certification, PartnerCourseGroup).</summary>
    [HttpGet("partners")]
    public async Task<ActionResult<IEnumerable<PartnerLookup>>> GetPartners()
    {
        var partners = await _repository.GetPartnersAsync();
        return Ok(partners);
    }

    /// <summary>Slim CourseGroup list for FK dropdowns (Course, PartnerCourseGroup).</summary>
    [HttpGet("course-groups")]
    public async Task<ActionResult<IEnumerable<CourseGroupLookup>>> GetCourseGroups()
    {
        var courseGroups = await _repository.GetCourseGroupsAsync();
        return Ok(courseGroups);
    }

    /// <summary>Slim Certification list for the Course certifications multiselect.</summary>
    [HttpGet("certifications")]
    public async Task<ActionResult<IEnumerable<CertificationLookup>>> GetCertifications()
    {
        var certifications = await _repository.GetCertificationsAsync();
        return Ok(certifications);
    }

    /// <summary>Slim JobCategory list for the Course job-categories multiselect.</summary>
    [HttpGet("job-categories")]
    public async Task<ActionResult<IEnumerable<JobCategoryLookup>>> GetJobCategories()
    {
        var jobCategories = await _repository.GetJobCategoriesAsync();
        return Ok(jobCategories);
    }

    /// <summary>Slim TrainingCenter list for the FeaturedPromoItem scheduler tabs.</summary>
    [HttpGet("training-centers")]
    public async Task<ActionResult<IEnumerable<TrainingCenterLookup>>> GetTrainingCenters()
    {
        var trainingCenters = await _repository.GetTrainingCentersAsync();
        return Ok(trainingCenters);
    }

    /// <summary>Slim Promotion2 list for the FeaturedPromoItem PromoCode autocomplete lookup.</summary>
    [HttpGet("promotions")]
    public async Task<ActionResult<IEnumerable<PromotionLookup>>> GetPromotions()
    {
        var promotions = await _repository.GetPromotionsAsync();
        return Ok(promotions);
    }
}
