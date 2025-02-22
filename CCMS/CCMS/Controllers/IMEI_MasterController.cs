using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CCMS.Models;
using CCMS.Service;

namespace CCMS.Controllers
{
    public class IMEI_MasterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IMEI_MasterController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: IMEI_Master
        public async Task<IActionResult> Activation()
        {
            return View(await _context.IMEI_Master.ToListAsync());
        }

        public IActionResult FilterJson(string uid)
        {
            var filteredDevices = _context.IMEI_Master
                .Where(d => d.UID.Contains(uid))
                .Select(d => new { d.UID }) // Only fetch UID
                .ToList();

            return Json(filteredDevices);
        }

        // Filter UID List
        public async Task<IActionResult> filter(string uid)
        {
            var devices = _context.IMEI_Master.AsQueryable();

            if (!string.IsNullOrEmpty(uid))
            {
                devices = devices.Where(d => d.UID.Contains(uid));
            }

            ViewBag.UIDs = await devices.Select(d => d.UID).Distinct().ToListAsync();
            return View("Index", await devices.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Active([FromBody] Dictionary<string, string> requestData)
        {
            if (!requestData.ContainsKey("uid"))
            {
                return Json(new { success = false, message = "Invalid request. UID is missing." });
            }

            string uid = requestData["uid"];
            var device = await _context.IMEI_Master.FindAsync(uid);

            if (device == null)
            {
                return Json(new { success = false, message = "Device not found!" });
            }

            // Update LiveDevice field
            device.LiveDevice = 1;
            Console.WriteLine(uid + " Activated: " + device.LiveDevice);

            // Save changes to the database
            _context.Update(device);
            await _context.SaveChangesAsync();

            // Return success response
            return Json(new { success = true, message = $"Device activated successfully for UID {uid}!" });
        }

        public IActionResult UidImeiBinding() // Changed method name
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetDeviceDetails(string uid)
        {
            var device = _context.IMEI_Master
                .Where(d => d.UID == uid)
                .Select(d => new
                {
                    uid = d.UID,
                    phase = d.Phase,
                    location = d.Location,
                    imei_no = d.IMEI_no
                })
                .FirstOrDefault();

            if (device == null)
            {
                return Json(new { error = "UID not found!" });
            }

            return Json(device);
        }

        [HttpPost]
        public IActionResult UpdateTestedDevice(IMEI_Master model)
        {
            var device = _context.IMEI_Master.FirstOrDefault(d => d.UID == model.UID);
            if (device == null)
            {
                TempData["ErrorMessage"] = "UID not found!";
                return RedirectToAction("UidImeiBinding");
            }

            device.Phase = model.Phase;
            device.Location = model.Location;
            device.IMEI_no = model.IMEI_no;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "IMEI Binded successfully!";
            return RedirectToAction("UidImeiBinding");
        }

        [HttpGet]
        public JsonResult GetUIDs(string query)
        {
            var matchedUIDs = _context.IMEI_Master
                .Where(d => d.UID.Contains(query))
                .Select(d => d.UID)
                .Distinct()
                .Take(10)
                .ToList();

            return Json(matchedUIDs);
        }

        [HttpGet]
        public async Task<IActionResult> GetDeviceByUID(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                TempData["ErrorMessage"] = "Please enter a UID.";
                return View("UidImeiBinding");
            }

            // Fetch the exact device if fully matched
            var device = await _context.IMEI_Master.FirstOrDefaultAsync(d => d.UID == uid);

            // Fetch all UIDs that match the entered string (partial search)
            var matchingDevices = await _context.IMEI_Master
                                                .Where(d => d.UID.Contains(uid))
                                                .ToListAsync();

            ViewBag.MatchingDevices = matchingDevices; // Pass filtered devices to the view

            if (device == null && matchingDevices.Count == 0)
            {
                TempData["ErrorMessage"] = "No matching UID found.";
                return View("UidImeiBinding");
            }

            return View("UidImeiBinding", device);
        }

        public IActionResult SiteDataEntry()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetSiteUIDs(string query)
        {
            var uids = _context.IMEI_Master
                .Where(d => d.UID.Contains(query))
                .Select(d => d.UID)
                .Distinct()
                .Take(10)
                .ToList();

            return Json(uids);
        }

        [HttpGet]
        public JsonResult GetDeviceData(string uid)
        {
            var device = _context.IMEI_Master.FirstOrDefault(d => d.UID == uid);
            if (device == null)
            {
                return Json(new { error = "Device not found" });
            }

            return Json(device);
        }

        public async Task<IActionResult> FetchDeviceByUID(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                TempData["ErrorMessage"] = "Please enter a UID.";
                return RedirectToAction(nameof(UidImeiBinding));
            }

            var device = await _context.IMEI_Master.FirstOrDefaultAsync(d => d.UID == uid);

            if (device == null)
            {
                TempData["ErrorMessage"] = "No device found for this UID.";
                return RedirectToAction(nameof(UidImeiBinding));
            }

            return View("UpdateDevice", device);
        }

        public async Task<IActionResult> EditDeviceByUID(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                TempData["ErrorMessage"] = "Please enter a UID.";
                return View();
            }

            var device = await _context.IMEI_Master.FirstOrDefaultAsync(d => d.UID == uid);

            if (device == null)
            {
                TempData["ErrorMessage"] = "No device found for this UID.";
                return View();
            }

            return View(device);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("UID, Phase, Location, IMEI_no")] IMEI_Master device)
        {
            if (id != device.UID)
            {
                return NotFound();
            }

            try
            {
                var existingDevice = await _context.IMEI_Master.FindAsync(id);
                if (existingDevice == null)
                {
                    return NotFound();
                }

                // Update only specific fields
                existingDevice.Phase = device.Phase;
                existingDevice.Location = device.Location;
                existingDevice.IMEI_no = device.IMEI_no;

                _context.Entry(existingDevice).Property(x => x.Phase).IsModified = true;
                _context.Entry(existingDevice).Property(x => x.Location).IsModified = true;
                _context.Entry(existingDevice).Property(x => x.IMEI_no).IsModified = true;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.IMEI_Master.Any(e => e.UID == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<IActionResult> UpdateDevice(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var device = await _context.IMEI_Master.FindAsync(id);
            if (device == null)
            {
                return NotFound();
            }

            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDevice(IMEI_Master model)
        {
            if (ModelState.IsValid)
            {
                var existingDevice = await _context.IMEI_Master.FindAsync(model.UID);
                if (existingDevice == null)
                {
                    return NotFound();
                }

                // Update only if the new value is provided
                existingDevice.Location = !string.IsNullOrEmpty(model.Location) ? model.Location : existingDevice.Location;
                existingDevice.Latitude = model.Latitude ?? existingDevice.Latitude;
                existingDevice.Longitude = model.Longitude ?? existingDevice.Longitude;
                existingDevice.SIM_No = !string.IsNullOrEmpty(model.SIM_No) ? model.SIM_No : existingDevice.SIM_No;
                existingDevice.IMEI_no = !string.IsNullOrEmpty(model.IMEI_no) ? model.IMEI_no : existingDevice.IMEI_no;
                existingDevice.NoOfStreetlight = !string.IsNullOrEmpty(model.NoOfStreetlight) ? model.NoOfStreetlight : existingDevice.NoOfStreetlight;
                existingDevice.FullLoad = model.FullLoad ?? existingDevice.FullLoad;
                existingDevice.Zone = !string.IsNullOrEmpty(model.Zone) ? model.Zone : existingDevice.Zone;
                existingDevice.Ward = !string.IsNullOrEmpty(model.Ward) ? model.Ward : existingDevice.Ward;

                // Explicitly attach and mark as modified
                _context.Attach(existingDevice);
                _context.Entry(existingDevice).State = EntityState.Modified;

                Console.WriteLine($"Updating UID: {existingDevice.UID}, Location: {existingDevice.Location}");

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Data Saved successfully!";
                return RedirectToAction("SiteDataEntry");
            }

            TempData["ErrorMessage"] = "Invalid input data!";
            return RedirectToAction("SiteDataEntry");
        }
    }
}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UpdateDevice(string id, [Bind("UID, Location, Latitude, Longitude, SIM_No, IMEI_no, NoOfStreetlight, FullLoad, Zone, Ward")] IMEI_Master device)
        //{
        //    if (id != device.UID)
        //    {
        //        return NotFound();
        //    }

        //    try
        //    {
        //        var existingDevice = await _context.IMEI_Master.FindAsync(id);
        //        if (existingDevice == null)
        //        {
        //            return NotFound();
        //        }

        //        // Update only selected fields
        //        existingDevice.Location = device.Location;
        //        existingDevice.Latitude = device.Latitude;
        //        existingDevice.Longitude = device.Longitude;
        //        existingDevice.SIM_No = device.SIM_No;
        //        existingDevice.IMEI_no = device.IMEI_no;
        //        existingDevice.NoOfStreetlight = device.NoOfStreetlight;
        //        existingDevice.FullLoad = device.FullLoad;
        //        existingDevice.Zone = device.Zone;
        //        existingDevice.Ward = device.Ward;

        //        // Mark these fields as modified
        //        _context.Entry(existingDevice).Property(x => x.Location).IsModified = true;
        //        _context.Entry(existingDevice).Property(x => x.Latitude).IsModified = true;
        //        _context.Entry(existingDevice).Property(x => x.Longitude).IsModified = true;
        //        _context.Entry(existingDevice).Property(x => x.SIM_No).IsModified = true;
        //        _context.Entry(existingDevice).Property(x => x.IMEI_no).IsModified = true;
        //        _context.Entry(existingDevice).Property(x => x.NoOfStreetlight).IsModified = true;
        //        _context.Entry(existingDevice).Property(x => x.FullLoad).IsModified = true;
        //        _context.Entry(existingDevice).Property(x => x.Zone).IsModified = true;
        //        _context.Entry(existingDevice).Property(x => x.Ward).IsModified = true;

        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(GetDeviceByUID));
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!_context.IMEI_Master.Any(e => e.UID == id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }
        //}

//public async Task<IActionResult> Active(string id)
//{
//    var device = await _context.IMEI_Master.FindAsync(id);

//    if (device == null)
//    {
//        TempData["ErrorMessage"] = "Device not found!";
//        return RedirectToAction(nameof(Index));
//    }

//     Update LiveDevice field
//    device.LiveDevice = 1;
//    Console.WriteLine(id + " Activated: " + device.LiveDevice);

//     Save changes to database
//    _context.Update(device);
//    await _context.SaveChangesAsync();

//     Store success message
//    TempData["SuccessMessage"] = "Device activated successfully!";

//     Redirect back to Index view
//    return RedirectToAction(nameof(Index));
//}




//public async Task<IActionResult> filter(string uid)
//{
//    var imeiList = from i in _context.IMEI_Master select i;

//    if (!string.IsNullOrEmpty(uid))
//    {
//        imeiList = imeiList.Where(i => i.UID.ToString().Contains(uid));
//    }

//    return View("Index", await imeiList.ToListAsync());
//}