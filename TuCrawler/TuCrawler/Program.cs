using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TuCrawler
{

    class Program
    {



        static void Main(string[] args)
        {
            var sw = File.CreateText(@"C:\Temp\TUAppointments.json");
            var courseData = "";
           


            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.Cookie] = "JSESSIONID=d5~C436F2F620CC3F6F48A0F021096DA3C7; TISS_LANG=en; ClarityID=d4598844749e490590751f35f266caa2; _tiss_session=bc522823e48a24984a7826c3b106e02e; dsrwid-285=6471; dsrwid-208=6471";
         
                string html = client.DownloadString(@"https://tiss.tuwien.ac.at/curriculum/public/curriculum.xhtml?dswid=6471&dsrid=285&semesterCode=2018W&semester=YEAR&key=67853&viewAcademicYear=true&dsrid=208");
                // feed the HTML to HTML Agility Pack
                var doc = new HtmlDocument();
                doc.LoadHtml(html);



                foreach (HtmlNode div in doc.DocumentNode.SelectNodes("//div"))
                {

                    foreach (var attr in div.Attributes)
                    {
                        
                        if (attr.Name.Equals("class") && attr.Value.Equals("courseTitle"))
                        {
                            var detailUrl = div.ChildNodes[0].Attributes["href"].Value;
                            var courseNumber = Regex.Match(detailUrl, @"\d+").Value;
                            var data = downloadCourseData(courseNumber);
                            if (!data.Equals("empty"))
                                courseData += "{" + data + "},";
                            
                        }
                    }
                }
                courseData = courseData.Substring(0, courseData.Length - 1);
                sw.Write("{ \"courses\":[" + courseData + "]}");
                sw.Close();
                
            }
               
            Console.ReadLine();
        }

        static String downloadCourseData(String courseNumber)
        {
            var courseString = "";
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.Cookie] = "JSESSIONID=d5~4B09F0549280A1B9E342B5A19442AE20; TISS_LANG=en; ClarityID=d4598844749e490590751f35f266caa2; _tiss_session=bc522823e48a24984a7826c3b106e02e; dsrwid-430=6084";
                //string url = @"https://tiss.tuwien.ac.at" + detailUrl + "&dsrid = 430";
                string url = @"https://tiss.tuwien.ac.at/course/courseDetails.xhtml?courseNr="+ courseNumber + "&dsrid=430";
                string html = client.DownloadString(url);
                // feed the HTML to HTML Agility Pack
                Thread.Sleep(300);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                try
                {
                    var title = doc.DocumentNode.SelectSingleNode("//title").InnerText;
                    Console.WriteLine(title);
                   
                    courseString += "\"dates\":[";
                    foreach (var table in doc.DocumentNode.SelectNodes("//table"))
                    {
                     
                        if (table.ChildNodes.Count() > 0 && table.ChildNodes[0].Attributes.Count > 0 && table.ChildNodes[0].Attributes[0].Value.Equals("j_id_2h:eventDetailDateTable_head"))
                            foreach (var row in table.ChildNodes[1].ChildNodes)
                            {
                                var appointment = "";
                                var colcount = 0;
                                foreach (var col in row.ChildNodes)
                                {
                                    var column = "other";
                                    switch (colcount)
                                    {
                                        case 0:
                                            column = "Weekday";
                                            break;
                                        case 1:
                                            column = "Date";
                                            break;
                                        case 2:
                                            column = "Time";
                                            break;
                                        case 3:
                                            column = "Room";
                                            break;
                                        case 4:
                                            column = "Lecture";
                                            break;
                                    }
                                    appointment += "\""+ column + "\":\"" + col.InnerText + "\",";
                                    colcount += 1;
                                }
                                //den letzten Beistrich abschneiden 
                                if (appointment.Length > 10)
                                    appointment = appointment.Substring(0, appointment.Length - 1);
                               

                                //Console.WriteLine(appointment);
                                courseString += "{" + appointment + "},";
                            }
                        
                    }
                    //letzten beistrich abschneiden wenn termine vorhanden waren.
                    if (courseString.Length > 10)
                        courseString = courseString.Substring(0, courseString.Length - 1);
                    courseString += "],";
                    courseString += "\"title\":\"" + title + "\"";
                }
                catch(Exception ex)
                { Console.WriteLine(ex.Message);
                    return "empty";
                }
            }
            return courseString;
        }

    }
}
