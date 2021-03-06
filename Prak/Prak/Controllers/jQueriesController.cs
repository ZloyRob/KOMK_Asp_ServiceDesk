﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Prak.Models;
using Microsoft.AspNet.Identity;
using MvcSiteMapProvider;


namespace Prak.Controllers
{
    //Контроллер отвечает за обработку входящих запросов, 
    //выполнение операций над моделью предметной области и выбор представлений для визуализации пользователю.
   // [MvcSiteMapNode(Title = "TestGlobal", ParentKey = "Home", Key = "jQuery", Roles = new string[] { "User" })]
    public class jQueriesController : Controller
    {
        //Создаем экземпляр класса контекста для взаимодействия с нашей бд 
        private KOMK_Main_v2Entities db = new KOMK_Main_v2Entities();

        public interface IRepository : IDisposable
        {
            List<AspNetUsers> GetUserList();
            List<AspNetUserRoles> GetUserRoleList();
            List<AspNetRoles> GetRoleList();
            AspNetUsers GetUserFromDb(string Id);
            List<jQuery> GetQueryList();

        }

        class Mydb : IRepository
        {
            private KOMK_Main_v2Entities db;

            public  Mydb()
            {
                this.db = new KOMK_Main_v2Entities();
            }
            public List<AspNetUsers> GetUserList()
            {
                return db.AspNetUsers.ToList();
            }

            public List<AspNetUserRoles> GetUserRoleList()
            {
                return db.AspNetUserRoles.ToList();
            }

            public List<AspNetRoles> GetRoleList()
            {
                return db.AspNetRoles.ToList();
            }

            public List<jQuery> GetQueryList()
            {
                return db.jQuery.ToList();
            }

            public AspNetUsers GetUserFromDb(string Id)
            {
                return db.AspNetUsers.Find(Id);
            }


            private bool disposed = false;

            public virtual void Dispose(bool disposing)
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        db.Dispose();
                    }
                }
                this.disposed = true;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        IRepository repo;

        public jQueriesController(IRepository r)
        {
            repo = r;
        }
        public jQueriesController()
        {
            repo = new Mydb();
        }
        // GET: jQueries
        // Метод Index, в нем мы задаем связи с какими таблицами нам будут нужны для оботражения данных в представлени
        //Результатом работы метода является вызов представления Index    
        //[MvcSiteMapNode(Title = "Test", ParentKey = "jQuery", Key = "jQueryIndex", Roles = new string[] { "User" })]   
        public ActionResult Index()
        {
            var jQuery = db.jQuery.Include(j => j.AspNetUsers).Include(j => j.AspNetUsers1).Include(j => j.hState);
            AspNetUsers userNow = db.AspNetUsers.Find(User.Identity.GetUserId());
            AspNetUserRoles usrol = db.AspNetUserRoles.Where(m => m.UserId == userNow.Id).First();
            AspNetRoles rol = db.AspNetRoles.Where(m => m.Id == usrol.RoleId).First();
            switch (rol.Name)
            {
                case "Admin": return View(jQuery.ToList());
                case "User": return View(jQuery.Where(m=>m.PersonId == userNow.Id).ToList());
            }
            return View(jQuery.ToList());
        }

        // GET: jQueries/Details/5
        // Метод Детали, имеет аргумент id, чтобы получать из базы данные только по 1 конкретной заявке
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //Производим поиск по id
            jQuery jQuery = db.jQuery.Find(id);
            if (jQuery == null)
            {
                return HttpNotFound();
            }
            return View(jQuery);
        }

        public List<AspNetUsers> GetAdmin()
        {
            List<AspNetUserRoles> usrol = repo.GetUserRoleList().Where(m => m.RoleId == repo.GetRoleList().Where(r => r.Name =="Admin").FirstOrDefault().Id).ToList();
            List<AspNetUsers> users = new List<AspNetUsers>();
            foreach(AspNetUserRoles ur in usrol)
            {
                users.Add(repo.GetUserFromDb(ur.UserId));
            }
            return users;
        }
        // GET: jQueries/Create
        // Используем ViewBag для того чтобы в представлении у нас были вместо вторичных ключей конкретные значения из связаных таблиц
        public ActionResult Create()
        {
            ViewBag.PersonId = new SelectList(db.AspNetUsers, "Id", "Fio");
            ViewBag.PersonSpId = new SelectList(GetAdmin(), "Id", "Fio");
            ViewBag.StateId = new SelectList(db.hState, "StateId", "Description");
            return View();
        }

        // POST: jQueries/Create
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost] //Обрабатываем данные полученые из представления
        [ValidateAntiForgeryToken] 
        public ActionResult Create([Bind(Include = "QueryId,DateOut,DateIn,DateModification,DeadLine,Text,StateId,PersonId,PersonSpId")] jQuery jQuery)
        {
            if (ModelState.IsValid)
            {
                //Заполняем необходимые для для создания заявки поля, которые не видит пользователь 
                jQuery.DateIn = DateTime.Parse(DateTime.Today.ToShortDateString());
                jQuery.DateModification = DateTime.Now;
                jQuery.DateModification = jQuery.DateModification.AddMilliseconds(-jQuery.DateModification.Millisecond);
                DateTime dmd = jQuery.DateModification;             
                jQuery.StateId = db.hState.First(m => m.Description == "Ожидает").StateId;
                jQuery.PersonId = User.Identity.GetUserId();
                db.jQuery.Add(jQuery);
                db.SaveChanges();

                jJournal jJur = new jJournal();
                string dmdstr = dmd.ToString("yyyy-MM-dd HH:mm:ss") + ".000";
                DateTime dmdn = DateTime.Parse(dmdstr);
                db = new KOMK_Main_v2Entities();
                jQuery jQ = db.jQuery.First(m => m.DateModification== dmdn);    

                jJur.Date= DateTime.Now;
                jJur.EventTypeId = db.hEventType.First(m => m.Description == "Создание заявки").EventTypeId; 
                jJur.WorkListId = null;                
                jJur.PersonId = User.Identity.GetUserId();
                jJur.QueryID = jQ.QueryId;
                jJur.Description = " Содержание:  "+jQ.Text;


                db.jJournal.Add(jJur);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            //Если данные не валидны то делаем тоже что и в методе GET
            ViewBag.PersonId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonId);
            ViewBag.PersonSpId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonSpId);
            ViewBag.StateId = new SelectList(db.hState, "StateId", "Description", jQuery.StateId);
            return View(jQuery);
        }



        // GET: jQueries/ChangeStatusQuery/5
        // Метод Смена статуса заявки, имеет аргумент id, чтобы производить действия над конкретной заявкой
        public ActionResult ChangeStatusQuery(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            jQuery jQuery = db.jQuery.Find(id);
            TempData["oldState"] = jQuery.StateId;
            if (jQuery == null)
            {
                return HttpNotFound();
            }
            ViewBag.PersonId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonId);
            ViewBag.PersonSpId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonSpId);
            ViewBag.StateId = new SelectList(db.hState, "StateId", "Description", jQuery.StateId);
            return View(jQuery);
        }

        // POST: jQueries/ChangeStatusQuery/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost] // Сохраняем изменения 
        [ValidateAntiForgeryToken]
        public ActionResult ChangeStatusQuery([Bind(Include = "QueryId,DateOut,DateIn,DateModification,DeadLine,Text,StateId,PersonId,PersonSpId")] jQuery jQuery)
        {
            if (ModelState.IsValid)
            {
                db.Entry(jQuery).State = EntityState.Modified;
                if (jQuery.StateId == db.hState.First(m => m.Description == "Выполнена").StateId || jQuery.StateId == db.hState.First(m => m.Description == "Отклонена").StateId)
                {
                    jQuery.DateOut= DateTime.Parse(DateTime.Today.ToShortDateString());
                }
                if (Convert.ToInt32(TempData["oldState"])!= jQuery.StateId) {
                    jJournal jJur = new jJournal();
                    jJur.Date = DateTime.Now;
                    jJur.EventTypeId = db.hEventType.First(m => m.Description == "Смена статуса заявки").EventTypeId;
                    jJur.WorkListId = null;
                    jJur.PersonId = User.Identity.GetUserId();
                    jJur.QueryID = jQuery.QueryId;
                    hState oldst = db.hState.Find(Convert.ToInt32(TempData["oldState"]));
                    hState newst = db.hState.Find(jQuery.StateId);
                    jJur.Description = "c " + oldst.Description + " на " + newst.Description;
                    db.jJournal.Add(jJur);
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PersonId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonId);
            ViewBag.PersonSpId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonSpId);
            ViewBag.StateId = new SelectList(db.hState, "StateId", "Description", jQuery.StateId);
            return View(jQuery);
        }

        // GET: jQueries/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            jQuery jQuery = db.jQuery.Find(id);
            if (jQuery == null)
            {
                return HttpNotFound();
            }
            ViewBag.PersonId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonId);
            ViewBag.PersonSpId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonSpId);
            ViewBag.StateId = new SelectList(db.hState, "StateId", "Description", jQuery.StateId);
            return View(jQuery);
        }

        // POST: jQueries/Edit/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в статье http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "QueryId,DateOut,DateIn,DateModification,DeadLine,Text,StateId,PersonId,PersonSpId")] jQuery jQuery)
        {
            if (ModelState.IsValid)
            {
                db.Entry(jQuery).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PersonId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonId);
            ViewBag.PersonSpId = new SelectList(db.AspNetUsers, "Id", "Fio", jQuery.PersonSpId);
            ViewBag.StateId = new SelectList(db.hState, "StateId", "Description", jQuery.StateId);
            return View(jQuery);
        }
        // GET: jQueries/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            jQuery jQuery = db.jQuery.Find(id);
            if (jQuery == null)
            {
                return HttpNotFound();
            }
            return View(jQuery);
        }

        // POST: jQueries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            jQuery jQuery = db.jQuery.Find(id);
            db.jQuery.Remove(jQuery);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


        public ActionResult AjaxTest()
        {
            ViewData["data"] = Request["data"];
            if (Request.IsAjaxRequest())
                return PartialView("_AjaxTestPartial");
            return View();
        }
        public double MyPow(int a)
        {
            return Math.Pow(a, 2);
        }

        public ActionResult SomeText()
        {
            ViewBag.Message = "My text in ViewBag.Message";

            return View("SomeText");
        }

        public ActionResult QueryIndex()
        {
            var modelQuery = repo.GetQueryList();
            ViewBag.Message = String.Format("{0}", modelQuery.Count);

            return View(modelQuery);
        }

    }
}
