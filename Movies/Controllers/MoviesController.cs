using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movies.Models;
using Movies.ViewModels;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Movies.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IToastNotification _toastNotification;
        private new List<string> _allowedextenstion = new List<string> { ".jpg", ".png" };
        private long _maxAllowedPosterSize = 1048576;

        public MoviesController(ApplicationDbContext context , IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        public async Task<IActionResult> Index()
        {
            var movies = await _context.movies.OrderByDescending(m=>m.Rate).ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Create()
        {
            var viewmodel = new MovieFormViewModel()
            {
                Genres = await _context.Genres.OrderBy(m=>m.Name).ToListAsync()
            };
            return View("MovieForm",viewmodel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm",model);
            }
            var files = Request.Form.Files;

            if (!files.Any())
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster","Please Select Poster");
                return View("MovieForm", model);
            }

            var poster = files.FirstOrDefault();
            

            if (!_allowedextenstion.Contains(Path.GetExtension(poster.FileName).ToLower()))
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Only .jpg or .png images are allowed");
                return View("MovieForm", model);
            }

            if (poster.Length > _maxAllowedPosterSize )
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB");
                return View("MovieForm", model);
            }

            using var datastream = new MemoryStream();
            await poster.CopyToAsync(datastream);

            var movie = new Movie
            {
                Title = model.Title,
                Rate = model.Rate,
                GenreId = model.GenreId,
                Year = model.Year,
                StoryLine = model.StoryLine,
                Poster = datastream.ToArray()
            };

            _context.movies.Add(movie);
            _context.SaveChanges();

            _toastNotification.AddSuccessToastMessage("Movie Created Successfully");
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.movies.FindAsync(id);

            if (movie == null)
                return NotFound();

            var viewmodel = new MovieFormViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                GenreId = movie.GenreId,
                Rate = movie.Rate,
                Year = movie.Year,
                StoryLine = movie.StoryLine,
                Poster = movie.Poster,
                Genres= await _context.Genres.OrderBy(m=>m.Name).ToListAsync()
            };

            return View("MovieForm", viewmodel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieFormViewModel model)
        {

            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }
           
            var movie = await _context.movies.FindAsync(model.Id);

            if (movie == null)
                return NotFound();

            var fiels = Request.Form.Files;
            if (fiels.Any())
            {
                var poster = fiels.FirstOrDefault();

                using var datastream = new MemoryStream();

                await poster.CopyToAsync(datastream);

                model.Poster = datastream.ToArray();

                if (!_allowedextenstion.Contains(Path.GetExtension(poster.FileName).ToLower()))
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Only .jpg or .png images are allowed");
                    return View("MovieForm", model);
                }

                if (poster.Length > _maxAllowedPosterSize)
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB");
                    return View("MovieForm", model);
                }

                movie.Poster = datastream.ToArray();
            }

            movie.Title = model.Title;
            movie.StoryLine = model.StoryLine;
            movie.Rate = model.Rate;
            movie.GenreId = model.GenreId;
            movie.Year = model.Year;

            _context.SaveChanges();

            _toastNotification.AddSuccessToastMessage("Movie Updated Successfully");
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.movies.Include(m => m.genre).SingleOrDefaultAsync(m=>m.Id == id);

            if (movie == null)
                return NotFound();


            return View(movie); 
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.movies.FindAsync(id);

            if (movie == null)
                return NotFound();

            _context.movies.Remove(movie);
            _context.SaveChanges();

            return Ok();
        }
    }
}
