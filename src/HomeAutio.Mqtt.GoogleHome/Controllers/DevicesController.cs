﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using HomeAutio.Mqtt.GoogleHome.ActionFilters;
using HomeAutio.Mqtt.GoogleHome.Models.State;
using HomeAutio.Mqtt.GoogleHome.Validation;
using HomeAutio.Mqtt.GoogleHome.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HomeAutio.Mqtt.GoogleHome.Controllers
{
    /// <summary>
    /// Devices controller.
    /// </summary>
    [Authorize]
    public class DevicesController : Controller
    {
        private readonly ILogger<DevicesController> _log;

        private readonly GoogleDeviceRepository _deviceRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DevicesController"/> class.
        /// </summary>
        /// <param name="logger">Logging instance.</param>
        /// <param name="deviceRepository">Device repository.</param>
        public DevicesController(
            ILogger<DevicesController> logger,
            GoogleDeviceRepository deviceRepository)
        {
            _log = logger;
            _deviceRepository = deviceRepository;
        }

        /// <summary>
        /// Index.
        /// </summary>
        /// <returns>Response.</returns>
        public IActionResult Index()
        {
            var model = _deviceRepository.GetAll();

            return View(model);
        }

        /// <summary>
        /// Create device.
        /// </summary>
        /// <returns>Response.</returns>
        [ImportModelState]
        public IActionResult Create()
        {
            var model = new DeviceViewModel();

            return View(model);
        }

        /// <summary>
        /// Create device.
        /// </summary>
        /// <param name="viewModel">View Model.</param>
        /// <returns>Response.</returns>
        [HttpPost]
        [ExportModelState]
        public IActionResult Create(DeviceViewModel viewModel)
        {
            if (_deviceRepository.Contains(viewModel.Id))
                ModelState.AddModelError("Id", "Device Id already exists");

            // Set new values
            var device = new Device
            {
                Id = viewModel.Id,
                RoomHint = viewModel.RoomHint,
                Type = viewModel.Type,
                WillReportState = viewModel.WillReportState,
                Name = new Models.NameInfo
                {
                    Name = viewModel.Name
                },
                Traits = new List<DeviceTrait>()
            };

            // Default names
            if (!string.IsNullOrEmpty(viewModel.DefaultNames))
                device.Name.DefaultNames = viewModel.DefaultNames.Split(',').Select(x => x.Trim()).ToList();
            else
                device.Name.DefaultNames = new List<string>();

            // Nicknames
            if (!string.IsNullOrEmpty(viewModel.Nicknames))
                device.Name.Nicknames = viewModel.Nicknames.Split(',').Select(x => x.Trim()).ToList();
            else
                device.Name.Nicknames = new List<string>();

            // Device Info
            if (!string.IsNullOrEmpty(viewModel.Manufacturer) ||
                !string.IsNullOrEmpty(viewModel.Model) ||
                !string.IsNullOrEmpty(viewModel.HwVersion) ||
                !string.IsNullOrEmpty(viewModel.SwVersion))
            {
                if (device.DeviceInfo == null)
                    device.DeviceInfo = new Models.DeviceInfo();

                device.DeviceInfo.Manufacturer = !string.IsNullOrEmpty(viewModel.Manufacturer) ? viewModel.Manufacturer : null;
                device.DeviceInfo.Model = !string.IsNullOrEmpty(viewModel.Model) ? viewModel.Model : null;
                device.DeviceInfo.HwVersion = !string.IsNullOrEmpty(viewModel.HwVersion) ? viewModel.HwVersion : null;
                device.DeviceInfo.SwVersion = !string.IsNullOrEmpty(viewModel.SwVersion) ? viewModel.SwVersion : null;
            }
            else
            {
                device.DeviceInfo = null;
            }

            // Final validation
            foreach (var error in DeviceValidator.Validate(device))
                ModelState.AddModelError(string.Empty, error);

            if (!ModelState.IsValid)
                return RedirectToAction("Create");

            // Save changes
            _deviceRepository.Add(device);
            _deviceRepository.Persist();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Delete device.
        /// </summary>
        /// <param name="deviceId">Device id.</param>
        /// <returns>Response.</returns>
        [HttpPost]
        public IActionResult Delete([Required] string deviceId)
        {
            if (!_deviceRepository.Contains(deviceId))
                return NotFound();

            _deviceRepository.Delete(deviceId);

            // Save changes
            _deviceRepository.Persist();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Edit device.
        /// </summary>
        /// <param name="deviceId">Device id.</param>
        /// <returns>Response.</returns>
        [ImportModelState]
        public IActionResult Edit([Required] string deviceId)
        {
            if (!_deviceRepository.Contains(deviceId))
                return NotFound();

            var device = _deviceRepository.Get(deviceId);
            var model = new DeviceViewModel
            {
                Id = device.Id,
                RoomHint = device.RoomHint,
                Type = device.Type,
                WillReportState = device.WillReportState,
                Name = device.Name.Name,
                DefaultNames = string.Join(',', device.Name.DefaultNames),
                Nicknames = string.Join(',', device.Name.Nicknames),
            };

            if (device.DeviceInfo != null)
            {
                model.Manufacturer = device.DeviceInfo.Manufacturer;
                model.Model = device.DeviceInfo.Model;
                model.HwVersion = device.DeviceInfo.HwVersion;
                model.SwVersion = device.DeviceInfo.SwVersion;
            }

            if (device.Traits != null)
            {
                model.Traits = device.Traits.Select(x => x.Trait);
            }

            return View(model);
        }

        /// <summary>
        /// Edit device.
        /// </summary>
        /// <param name="deviceId">Device id.</param>
        /// <param name="viewModel">View Model.</param>
        /// <returns>Response.</returns>
        [HttpPost]
        [ExportModelState]
        public IActionResult Edit([Required] string deviceId, DeviceViewModel viewModel)
        {
            if (!_deviceRepository.Contains(deviceId))
                return NotFound();

            // Set new values
            var device = _deviceRepository.Get(deviceId);
            device.Id = viewModel.Id;
            device.RoomHint = viewModel.RoomHint;
            device.Type = viewModel.Type;
            device.WillReportState = viewModel.WillReportState;
            device.Name.Name = viewModel.Name;

            // Default names
            if (!string.IsNullOrEmpty(viewModel.DefaultNames))
                device.Name.DefaultNames = viewModel.DefaultNames.Split(',').Select(x => x.Trim()).ToList();
            else
                device.Name.DefaultNames = new List<string>();

            // Nicknames
            if (!string.IsNullOrEmpty(viewModel.Nicknames))
                device.Name.Nicknames = viewModel.Nicknames.Split(',').Select(x => x.Trim()).ToList();
            else
                device.Name.Nicknames = new List<string>();

            // Device Info
            if (!string.IsNullOrEmpty(viewModel.Manufacturer) ||
                !string.IsNullOrEmpty(viewModel.Model) ||
                !string.IsNullOrEmpty(viewModel.HwVersion) ||
                !string.IsNullOrEmpty(viewModel.SwVersion))
            {
                if (device.DeviceInfo == null)
                    device.DeviceInfo = new Models.DeviceInfo();

                device.DeviceInfo.Manufacturer = !string.IsNullOrEmpty(viewModel.Manufacturer) ? viewModel.Manufacturer : null;
                device.DeviceInfo.Model = !string.IsNullOrEmpty(viewModel.Model) ? viewModel.Model : null;
                device.DeviceInfo.HwVersion = !string.IsNullOrEmpty(viewModel.HwVersion) ? viewModel.HwVersion : null;
                device.DeviceInfo.SwVersion = !string.IsNullOrEmpty(viewModel.SwVersion) ? viewModel.SwVersion : null;
            }
            else
            {
                device.DeviceInfo = null;
            }

            // Final validation
            foreach (var error in DeviceValidator.Validate(device))
                ModelState.AddModelError(string.Empty, error);

            if (!ModelState.IsValid)
                return RedirectToAction("Edit", new { deviceId });

            // Save changes
            _deviceRepository.Persist();

            return RedirectToAction("Index");
        }
    }
}
