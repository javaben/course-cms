using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/featured-promo-items")]
public class FeaturedPromoItemsController : ControllerBase
{
    private readonly IFeaturedPromoItemRepository _repository;

    public FeaturedPromoItemsController(IFeaturedPromoItemRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Board query — filtered by the active TrainingCenter tab and Monday–Sunday week.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<FeaturedPromoItem>>> Query([FromBody] FeaturedPromoItemQuery query, CancellationToken ct)
        => Ok(await _repository.QueryAsync(query, ct));

    /// <summary>Single item by pkid.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FeaturedPromoItem>> GetById(int id, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Create an item. pkid is auto-generated (IDENTITY). 409 if the (date, center, slot) is taken.</summary>
    [HttpPost]
    public async Task<ActionResult<FeaturedPromoItem>> Create([FromBody] FeaturedPromoItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (await _repository.SlotTakenAsync(request.ScheduleOn, request.TrainingCenterPkid, request.Slot, null, ct))
            return Conflict($"版位 {request.Slot} 於 {request.ScheduleOn:yyyy-MM-dd} 已被使用。");

        var created = await _repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update an item. pkid (in body) identifies the record. 409 on slot clash, 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult> Update([FromBody] FeaturedPromoItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (await _repository.SlotTakenAsync(request.ScheduleOn, request.TrainingCenterPkid, request.Slot, request.Pkid, ct))
            return Conflict($"版位 {request.Slot} 於 {request.ScheduleOn:yyyy-MM-dd} 已被使用。");

        var updated = await _repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete an item by pkid. 404 if not found.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Move a row between slots (+1 down, -1 up), swapping with the occupant if any. 400 if out of range.</summary>
    [HttpPost("{id:int}/move")]
    public async Task<ActionResult> Move(int id, [FromBody] MoveSlotRequest request, CancellationToken ct)
    {
        var moved = await _repository.MoveSlotAsync(id, request.Direction, ct);
        return moved ? NoContent() : BadRequest("無法移動版位（超出範圍或找不到資料）。");
    }
}

/// <summary>Body for the slot-move action: +1 moves the row down a slot, -1 moves it up.</summary>
public class MoveSlotRequest
{
    public int Direction { get; set; }
}
