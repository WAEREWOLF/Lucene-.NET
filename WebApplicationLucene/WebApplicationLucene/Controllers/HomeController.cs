using FizzWare.NBuilder.Dates;
using iTextSharp.text.pdf.parser;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WebApplicationLucene.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        
        public ActionResult Indexing()
        { 
            string description = Request.Params["description"];
            HttpPostedFileBase filePicture = Request.Files["filePicture"];
            HttpPostedFileBase fileSpec = Request.Files["fileSpec"];
            
            if (description != null && filePicture != null && fileSpec != null)
            {
                //create file path 
                string fileName = filePicture.FileName;
                string pathFile = Server.MapPath("~/Content/images/");               
                string pathImage = System.IO.Path.Combine(pathFile, fileName);

                string fileSpecName = fileSpec.FileName;                
                string pathSpec = System.IO.Path.Combine(pathFile, fileSpecName);

                var fileContents = System.IO.File.ReadAllText(pathSpec);

                //saving image to server
                filePicture.SaveAs(pathImage);
                //Create Index : 
                string pathIndexFolder = Server.MapPath("~/Content/indexDirectory");
                Directory directory = FSDirectory.Open(pathIndexFolder);                

                Analyzer analyser = new Lucene.Net.Analysis.WhitespaceAnalyzer(); 
                bool create = !IndexReader.IndexExists(directory);
                //initialize indexWriter
                IndexWriter indexWriter = new IndexWriter(directory, analyser, create, IndexWriter.MaxFieldLength.UNLIMITED);
                //intialize document
                Document doc = new Document();
                //Create fiels
                Field field1 = new Field("name", fileName, Field.Store.YES, Field.Index.ANALYZED);
                Field field2 = new Field("description", description, Field.Store.YES, Field.Index.ANALYZED);
                Field field3 = new Field("content", fileContents, Field.Store.YES, Field.Index.ANALYZED);

                // remove older index entry
                var searchQuery = new TermQuery(new Term("name", fileName));
                indexWriter.DeleteDocuments(searchQuery);
                //add new index entry
                //add field to document
                doc.Add(field1);
                doc.Add(field2);
                doc.Add(field3);
                //add document to index Directory
                indexWriter.AddDocument(doc);
                //free indexWriter
                indexWriter.Dispose();
            }
            return View("Index");
        }

        public string Searching()
        {
           string dataHTML = "";
           string pathIndexFolder = Server.MapPath("~/Content/indexDirectory");
           string textToSearch = Request.Params["textToSearch"];
           //Create Index :          
           Directory directory = FSDirectory.Open(pathIndexFolder);
           IndexSearcher searcher = new IndexSearcher(directory, true);
           Analyzer analyser = new Lucene.Net.Analysis.WhitespaceAnalyzer( );
           QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "content", analyser);
           Lucene.Net.Search.Query query = parser.Parse(textToSearch.Trim());

           var hits_limit = 40;
           var hits = searcher.Search(query, hits_limit).ScoreDocs;

            foreach(var hit in hits)
            {
                var doc = searcher.Doc(hit.Doc);
               dataHTML += "<div class='col-lg-3'>";
               dataHTML += "<div class='panel panel-default'>";
               dataHTML += string.Format("<div class='panel-body'><center><img src='/Content/images/{0}' width='150px' height='150'/></center>", doc.Get("name"));
               dataHTML += string.Format("<hr><p><b>Image Name : </b>{0}</p><p><b>Description : </b>{1}</p><p><b>Specification file: </b>{2}</p></div></div></div>", doc.Get("name"), doc.Get("description"), doc.Get("content"));   
            }
           searcher.Dispose();
           return dataHTML;
        }
        public ActionResult DeleteIndexes()
        {
            string pathIndexFolder = Server.MapPath("~/Content/indexDirectory");
            Directory directory = FSDirectory.Open(pathIndexFolder);
            var reader = IndexReader.Open(directory, false);
            var docs = new List<Document>();
            //get all documents inside the index Directory
            var term = reader.TermDocs();
            //Fetch all available documents 
            while (term.Next())
            {
                //removing document from index Directory
                reader.DeleteDocument(term.Doc);
            }
            //free reader
            reader.Dispose();
            return View();
        }
    }
}