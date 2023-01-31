using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Diagnostics;
using Assignment1.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using System.Drawing;
using System.Text;
using System.Reflection.Metadata;
using Microsoft.Identity.Client;
using System.IO;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Generic;

namespace Assignment1.Controllers
{

    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult Index()
        {
            string Date = SetDate();
            string Ip_address = SetIpAddress();
            Console.WriteLine("NO:", Ip_address);
            string Time = SetTime();
            ViewData["string"] = AddDateTimeIp(Date, Time, Ip_address);
            return View();
        }

        public IActionResult Privacy()
        {
            ViewData["string"] = GetUserData();
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public string SetIpAddress()
        {
            string IpAddress = Response.HttpContext.Connection.RemoteIpAddress.ToString();
            return IpAddress;

        }
        public string SetDate()
        {
            string Date = DateTime.Now.ToString("dd-MM-yyyy");
            return Date;
        }
        public string SetTime()
        {
            string Time = DateTime.Now.ToString("HH:mm:ss");
            return Time;
        }
        public string AddDateTimeIp(string Date, string Time, string IpAddress)
        {
            MongoClient clientDb = new MongoClient("mongodb://localhost:27017");

            var userDb = clientDb.GetDatabase("UserDataCollection");
            var collection = userDb.GetCollection<BsonDocument>("Info");
            var document = new BsonDocument { { "Date", Date }, { "Time", Time }, { "IP", IpAddress } };
            collection.InsertOne(document);
            return "string";
        }
        public string GetUserData()
        {
            MongoClient clientDb = new MongoClient("mongodb://localhost:27017");

            var user = clientDb.GetDatabase("UserDataCollection");
            var collect = user.GetCollection<BsonDocument>("Info");

            var listDb = collect.Find(new BsonDocument()).ToList();
            BsonDocument document2 = new BsonDocument();
            foreach (var item in listDb)
            {
                document2 = item;
            }
            return $"Date :{document2["Date"].ToString()}, Time : {document2["Time"].ToString()},Ip Address :{document2["IP"].ToString()}";
        }


        [HttpGet]
        public IActionResult CreateForm()
        {
            return View();
        }
        [HttpPost]
        public IActionResult CreateForm(FormDetails product)
        {
            MongoClient dbClient = new MongoClient("mongodb://localhost:27017");
            var dbUser = dbClient.GetDatabase("UserFormData");
            var collection = dbUser.GetCollection<BsonDocument>("data");
            var document = new BsonDocument { { "Name", product.Name }, { "Country", product.Country } };
            collection.InsertOne(document);

            return Redirect("/");
        }

        public ActionResult ViewInfo()
        {
            MongoClient Client = new MongoClient("mongodb://localhost:27017");
            var db = Client.GetDatabase("UserFormData");
            var collection = db.GetCollection<BsonDocument>("data");
            var dbList = collection.Find(new BsonDocument()).ToList();
            BsonDocument document2 = new BsonDocument();
            foreach (var item in dbList)
            {
                document2 = item;
            }
            FormDetails form = new FormDetails();
            form.Name = document2["Name"].ToString();
            form.Country = document2["Country"].ToString();
            return View("ViewInfo", form);
        }



        public string DisplayCountry(string Country)
        {
            return (Country);
        }
        public IActionResult AddImage()
        {
            return View();
        }
        [HttpPost]
        public IActionResult AddImage(CreateImage item)
        {
            MongoClient dbClient = new MongoClient("mongodb://localhost:27017");
            var database = dbClient.GetDatabase("Images");
            var collection = database.GetCollection<BsonDocument>("data");
            GridFSBucket bucket = new GridFSBucket(database);
            var options = new GridFSUploadOptions
            {
                ChunkSizeBytes = 516096, // 504KB
                Metadata = new BsonDocument
    {
        { "resolution", "1080P" },
        { "copyrighted", true }
    }
            };
            
            using var stream =  bucket.OpenUploadStream(item.Title, options);
            var id = stream.Id;
            item.Image.CopyTo(stream);
            stream.Close();
            var document = new BsonDocument { { "Title", item.Title }, { "Description", item.Description }, { "Id", id } };
            collection.InsertOne(document);

            return Redirect("/");

        }
        public ActionResult DisplayContent()
        {
            MongoClient dbClient = new MongoClient("mongodb://localhost:27017");
            var database= dbClient.GetDatabase("Images");
            GridFSBucket bucket = new GridFSBucket(database);
            var collection = database.GetCollection<BsonDocument>("data");
            var dbList = collection.Find(new BsonDocument()).ToList();
            BsonDocument doc = new BsonDocument();
            CreateImage ImageData = new CreateImage();
            List<CreateImage> ImageList = new List<CreateImage>();
            foreach (var item in dbList)
            {
                doc = item;
                var Id = doc["Id"];
                var byteArray = bucket.DownloadAsBytes(Id);
                string Image = Convert.ToBase64String(byteArray);
                ImageData.Url = string.Format("data:image/png;base64,{0}", Image);
                ImageData.Description = doc["Description"].ToString();
                ImageData.EditId = (ObjectId)Id;
                ImageList.Add(ImageData);
            }
            return View(ImageData);
        }

        [HttpPost]
        public IActionResult NewImage(string EditId, CreateImage item)
        {

            MongoClient dbClient = new MongoClient("mongodb://localhost:27017");
            var database = dbClient.GetDatabase("Images");
            var collection = database.GetCollection<BsonDocument>("data");
            GridFSBucket bucket = new GridFSBucket(database);
            var options = new GridFSUploadOptions
            {
                ChunkSizeBytes = 516096, // 504KB
                Metadata = new BsonDocument
        {
            { "resolution", "1080P" },
            { "copyrighted", true }
        }
            };
            ObjectId.TryParse(EditId, out ObjectId id);
            var filter = Builders<BsonDocument>.Filter.Eq("Id", id);
            var doc = collection.Find(filter).FirstOrDefault();
            bucket.Delete(id);
            collection.DeleteOne(filter);
            item.Title = doc["Title"].ToString();
            using var stream = bucket.OpenUploadStream(item.Title, options);
            item.Image.CopyTo(stream);
            stream.Close();
            var document = new BsonDocument { { "Title", doc["Title"].ToString() }, { "Description", doc["Description"].ToString() }, { "Id", stream.Id } };
            collection.InsertOne(document);
            return Redirect("/");


        }
   
        [HttpPost]
        public IActionResult NewDescription(string EditId, string NewDesc)
        {
            MongoClient Client = new MongoClient("mongodb://localhost:27017");
            var db = Client.GetDatabase("Images");
            var collect = db.GetCollection<BsonDocument>("data");
            GridFSBucket bucket = new GridFSBucket(db);
            var options = new GridFSUploadOptions
            {
                ChunkSizeBytes = 255 * 1024,
                Metadata = new BsonDocument
        {
            { "resolution", "1080P" },
                        { "copyrighted", true }
        }
            };
            ObjectId.TryParse(EditId, out ObjectId oid);
            var filter = Builders<BsonDocument>.Filter.Eq("Id", oid);
            var doc = collect.Find(filter).FirstOrDefault();
            collect.DeleteOne(filter);
            var doc1 = new BsonDocument { { "Title", doc["Title"].ToString() }, { "Description", NewDesc }, { "Id", oid } };
            collect.InsertOne(doc1);
            return Redirect("/");

        }



    }
}