using CreekRiver.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// allows passing datetimes without time zone data 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// allows our api endpoints to access the database through Entity Framework Core
builder.Services.AddNpgsql<CreekRiverDbContext>(builder.Configuration["CreekRiverDbConnectionString"]);

// Set the JSON serializer options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CAMPSITE ENDPOINTS
app.MapGet("/api/campsites", (CreekRiverDbContext db) =>
{
    return db.Campsites.ToList();
});

app.MapGet("/api/campsites/{id}", (CreekRiverDbContext db, int id) =>
{
    return db.Campsites.Include(c => c.CampsiteType).Single(c => c.Id == id);
});

app.MapPost("/api/campsites", (CreekRiverDbContext db, Campsite campsite) =>
{
    db.Campsites.Add(campsite);
    db.SaveChanges();
    return Results.Created($"/api/campsites/{campsite.Id}", campsite);
});

app.MapDelete("/api/campsites/{id}", (CreekRiverDbContext db, int id) =>
{
    Campsite campsite = db.Campsites.SingleOrDefault(campsite => campsite.Id == id);
    if (campsite == null)
    {
        return Results.NotFound();
    }
    db.Campsites.Remove(campsite);
    db.SaveChanges();
    return Results.NoContent();

});

app.MapPut("/api/campsites/{id}", (CreekRiverDbContext db, int id, Campsite campsite) =>
{
    Campsite campsiteToUpdate = db.Campsites.SingleOrDefault(campsite => campsite.Id == id);
    if (campsiteToUpdate == null)
    {
        return Results.NotFound();
    }
    campsiteToUpdate.Nickname = campsite.Nickname;
    campsiteToUpdate.CampsiteTypeId = campsite.CampsiteTypeId;
    campsiteToUpdate.ImageUrl = campsite.ImageUrl;

    db.SaveChanges();
    return Results.NoContent();
});

// RESERVATION ENDPOINTS
app.MapGet("/api/reservations", (CreekRiverDbContext db) =>
{
    return db.Reservations
        .Include(r => r.UserProfile)
        .Include(r => r.Campsite)
        .ThenInclude(c => c.CampsiteType)
        .OrderBy(res => res.CheckinDate)
        .ToList();
});

app.MapPost("/api/reservations", (CreekRiverDbContext db, Reservation newRes) =>
{
    try
    {
        db.Reservations.Add(newRes);
        db.SaveChanges();
        return Results.Created($"/api/reservations/{newRes.Id}", newRes);
    }
    catch (DbUpdateException)
    {
        return Results.BadRequest("Invalid data submitted");
    }
});

app.MapDelete("/api/reservations/{id}", (CreekRiverDbContext db, int resId) =>
{
    Reservation reservation = db.Reservations.SingleOrDefault(res => res.Id == resId);
    if (reservation == null)
    {
        return Results.NotFound();
    }
    db.Reservations.Remove(reservation);
    db.SaveChanges();
    return Results.NoContent();
    //return Results.Ok(db.Reservations);
});

//USER PROFILE ENDPOINTS
app.MapGet("/api/userprofiles", (CreekRiverDbContext db) =>
{
    return db.UserProfiles
    .Include(up => up.Reservations)
    .ThenInclude(r => r.Campsite)
    .ThenInclude(c => c.CampsiteType)
    .ToList();
});

app.MapPost("/api/userprofiles", (CreekRiverDbContext db, UserProfile newUP) =>
{
    try
    {
        db.UserProfiles.Add(newUP);
        db.SaveChanges();
        return Results.Created($"/api/reservations/{newUP.Id}", newUP);
    }
    catch (DbUpdateException)
    {
        return Results.BadRequest("Invalid data submitted");
    }
});

app.MapDelete("/api/userprofiles/{id}", (CreekRiverDbContext db, int upId) =>
{
    UserProfile profileToDelete = db.UserProfiles.SingleOrDefault(p => p.Id == upId);
    if (profileToDelete == null)
    {
        return Results.NotFound();
    }
    db.UserProfiles.Remove(profileToDelete);
    db.SaveChanges();
    return Results.Ok(db.UserProfiles);
});

app.MapPut("/api/userprofiles/{id}", (CreekRiverDbContext db, int upId, UserProfile profile) =>
{
    UserProfile profileToUpdate = db.UserProfiles.SingleOrDefault(p => p.Id == upId);
    if (profileToUpdate == null)
    {
        return Results.NotFound();
    }
    profileToUpdate.FirstName = profile.FirstName;
    profileToUpdate.LastName = profile.LastName;
    profileToUpdate.Email = profile.Email;
    db.SaveChanges();
    return Results.Ok(profileToUpdate);
});

// CAMPSITE TYPE ENDPOINTS
app.MapGet("/api/campsitetypes", (CreekRiverDbContext db) =>
{
    return db.CampsiteTypes;
});

app.MapDelete("/api/campsitetypes{id}", (CreekRiverDbContext db, int ctId) =>
{
    CampsiteType typeToDelete = db.CampsiteTypes.SingleOrDefault(ct => ct.Id == ctId);
    if (typeToDelete == null)
    {
        return Results.NotFound();
    }
    db.CampsiteTypes.Remove(typeToDelete);
    db.SaveChanges();
    return Results.Ok(db.CampsiteTypes);
});

app.MapPut("/api/campsitetypes/{id}", (CreekRiverDbContext db, int ctId, CampsiteType type) =>
{
    CampsiteType typeToUpdate = db.CampsiteTypes.SingleOrDefault(ct => ct.Id == ctId);
    if (typeToUpdate == null)
    {
        return Results.NotFound();
    }
    typeToUpdate.CampsiteTypeName = type.CampsiteTypeName;
    typeToUpdate.MaxReservationDays = type.MaxReservationDays;
    typeToUpdate.FeePerNight = type.FeePerNight;
    db.SaveChanges();
    return Results.Ok(typeToUpdate);
});

app.MapPost("/api/campsitetypes", (CreekRiverDbContext db, CampsiteType type) =>
{
    try
    {
        db.CampsiteTypes.Add(type);
        db.SaveChanges();
        return Results.Created($"/api/campsitetypes/{type.Id}", type);
    }
    catch (DbUpdateException)
    {
        return Results.BadRequest("Invalid data submitted");
    }
});

app.Run();
