﻿using System;
using System.Collections.Generic;
using System.Linq;
using DAL;
using DAL.Models;
using DAL.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITIS_Bet.Controllers
{
    public class HomeController : Controller
    {
        private readonly Database _db;

        public HomeController(Database db) =>
            _db = db;

        public IActionResult Index(string sport = null)
        {
            var matches = _db.Matches.Where(a =>
                a.Status.Equals(MatchStatus.Active) || a.Status.Equals(MatchStatus.Waiting));

            if (sport != null) matches = matches.Where(a => a.Sport.Equals((Sport) Enum.Parse(typeof(Sport), sport)));

            matches = matches.Include(a=>a.Bets.OrderBy(b=>b.Description));


            var map = new Dictionary<Sport, Dictionary<string, List<Matches>>>();
            foreach (var match in matches)
            {
                if (!map.ContainsKey(match.Sport))
                    map.Add(match.Sport, new Dictionary<string, List<Matches>>());

                if (!map[match.Sport].ContainsKey(match.Tournament))
                    map[match.Sport].Add(match.Tournament, new List<Matches>());

                map[match.Sport][match.Tournament].Add(match);
            }

            if (sport == null) ViewData["sport"] = "";
            else
                ViewData["sport"] = sport;


            return View(map);
        }

        public IActionResult Privacy() => View();
    }
}
