﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IC2.Models;
using System.IO;
using System.Transactions;
using System.Globalization;
using System.Text;
using IC2.Helpers;

namespace IC2.Controllers
{
    public class AjustesObjecionController : Controller
    {
        ICPruebaEntities db = new ICPruebaEntities();
        IDictionary<int, string> meses = new Dictionary<int, string>() {
            {1, "ENERO"}, {2, "FEBRERO"},
            {3, "MARZO"}, {4, "ABRIL"},
            {5, "MAYO"}, {6, "JUNIO"},
            {7, "JULIO"}, {8, "AGOSTO"},
            {9, "SEPTIEMBRE"}, {10, "OCTUBRE"},
            {11, "NOVIEMBRE"}, {12, "DICIEMBRE"}
        };
        public ActionResult Index()
        {
            HomeController oHome = new HomeController();
            ViewBag.Linea = "Linea";
            ViewBag.IdLinea = (int)Session["IdLinea"];
            List<Submenu> lista = new List<Submenu>();
            List<Menu> listaMenu = new List<Menu>();
            lista = oHome.obtenerMenu((int)Session["IdLinea"]);
            listaMenu = oHome.obtenerMenuPrincipal((int)Session["IdLinea"]);
            ViewBag.Lista = lista;
            ViewBag.ListaMenu = listaMenu;
            return View(ViewBag);
        }

        public JsonResult CargarCSV(HttpPostedFileBase archivoCSV, int lineaNegocio)
        {
            List<string> listaErrores = new List<string>();
            IEnumerable<string> lineas = null;
            object respuesta = null;
            int totalProcesados = 0;
            int lineaActual = 2;
            bool status = false;
            string exception = "Error, se presento un error inesperado.";
            DateTime fechaContable = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            try {
                /// Se salta la primera linea, el encabezado
                List<string> csvData = new List<string>();
                using (System.IO.StreamReader reader = new System.IO.StreamReader(archivoCSV.InputStream, Encoding.Default)) {
                    while (!reader.EndOfStream) {
                        csvData.Add(reader.ReadLine());
                    }
                }

                lineas = csvData.Skip(1);


                totalProcesados = lineas.Count();
                using (TransactionScope scope = new TransactionScope()) {
                    foreach (string linea in lineas) {
                        var lineaSplit = linea.Split(';');
                        if (lineaSplit.Count() == 10) {
                            try {
                                ajustesObjecion aObj = new ajustesObjecion();

                                aObj.sentido = lineaSplit[0];
                                aObj.sociedad = lineaSplit[1];
                                aObj.trafico = lineaSplit[2];
                                aObj.servicio = lineaSplit[3];
                                aObj.deudorAcreedor = lineaSplit[4];
                                aObj.operador = lineaSplit[5];
                                aObj.grupo = lineaSplit[6];

                                aObj.periodo = Convert.ToDateTime(lineaSplit[7]);
                                aObj.importe = decimal.Parse(lineaSplit[8]);
                                aObj.moneda = lineaSplit[9];
                                aObj.lineaNegocio = lineaNegocio;
                                aObj.periodoContable = fechaContable;

                                var result = db.spValidaAjustesObj(aObj.sentido, aObj.sociedad, aObj.trafico,
                                    aObj.servicio, aObj.deudorAcreedor, aObj.operador, aObj.grupo, lineaNegocio).ToList();

                                aObj.idSociedad = result[0].idSociedad;
                                aObj.idTrafico = result[0].idTrafico;
                                aObj.idServicio = result[0].idServicio;
                                aObj.idDeudorAcreedor = result[0].idDeudorAcreedor;
                                aObj.idOperador = result[0].idOperador;
                                aObj.idGrupo = result[0].idGrupo;

                                if (result[0].idStatus == 1)
                                    aObj.activo = 1;
                                else
                                    aObj.activo = 0;

                                db.ajustesObjecion.Add(aObj);
                                Log log = new Log();
                                log.insertaNuevoOEliminado(aObj, "Nuevo", "ajustesObjecion.html", Request.UserHostAddress);

                            } catch (FormatException) {
                                listaErrores.Add("Línea " + lineaActual + ": Campo con formato erróneo.");
                            }
                        } else {
                            listaErrores.Add("Línea " + lineaActual + ": Número de campos insuficiente.");
                        }
                        ++lineaActual;
                    }
                    // Termina exitosamente la transaccion
                    db.SaveChanges();
                    scope.Complete();
                    exception = "Datos cargados con éxito";
                    status = true;
                }
            } catch (FileNotFoundException) {
                exception = "El archivo Selecionado aún no existe en el Repositorio.";
                status = false;
            } catch (UnauthorizedAccessException) {
                exception = "No tiene permiso para acceder al archivo actual.";
                status = false;
            } catch (IOException e) when ((e.HResult & 0x0000FFFF) == 32) {
                exception = "Falta el nombre del archivo, o el archivo o directorio está en uso.";
                status = false;
            } catch (TransactionAbortedException) {
                exception = "Transacción abortada. Se presentó un error.";
                status = false;
            } finally {
                respuesta = new
                {
                    success = true,
                    results = listaErrores,
                    mensaje = exception,
                    totalProcesados,
                    status
                };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaPeriodoContable(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from periodos in db.ajustesObjecion
                            where periodos.lineaNegocio == lineaNegocio
                            group periodos by periodos.periodoContable into g
                            select new
                            {
                                Id = g.Key,
                                Periodo = g.Key
                            };

                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        elemento.Id,
                        Periodo = elemento.Periodo.Year + "-" + elemento.Periodo.Month + "-" + elemento.Periodo.Day,
                        Fecha = elemento.Periodo.Year + " " + meses[elemento.Periodo.Month]
                    });
                }

                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }
        public JsonResult LlenaPeriodoDato(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from periodos in db.ajustesObjecion
                            where periodos.lineaNegocio == lineaNegocio
                            group periodos by periodos.periodo into g
                            select new
                            {
                                Id = g.Key,
                                Periodo = g.Key
                            };

                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        elemento.Id,
                        Periodo = elemento.Periodo.Year + "-" + elemento.Periodo.Month + "-" + elemento.Periodo.Day,
                        Fecha = elemento.Periodo.Year + " " + meses[elemento.Periodo.Month]
                    });
                }

                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaGrid(int? lineaNegocio, DateTime periodoContable, string sentido, List<string> trafico, DateTime? periodo, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;

            int total;
            try {
                var datos = from oDatos in db.ajustesObjecion
                            where
                                oDatos.activo == 1 &&
                                oDatos.lineaNegocio == lineaNegocio &&
                                oDatos.periodoContable == periodoContable
                            select new
                            {
                                oDatos.Id,
                                oDatos.sentido,
                                oDatos.idSociedad,
                                oDatos.sociedad,
                                oDatos.idTrafico,
                                oDatos.trafico,
                                oDatos.idServicio,
                                oDatos.servicio,
                                oDatos.idDeudorAcreedor,
                                oDatos.deudorAcreedor,
                                oDatos.idOperador,
                                oDatos.operador,
                                oDatos.idGrupo,
                                oDatos.grupo,
                                oDatos.periodo,
                                oDatos.importe,
                                oDatos.moneda
                            };

                if (!string.IsNullOrEmpty(sentido) && trafico != null && periodo != null) {
                    datos = datos.Where(c => c.sentido == sentido && c.periodo.Year == periodo.Value.Year && c.periodo.Month == periodo.Value.Month && trafico.Contains(c.trafico));
                } else if (!string.IsNullOrEmpty(sentido) && trafico != null) {
                    datos = datos.Where(c => c.sentido == sentido && trafico.Contains(c.trafico));
                } else if (!string.IsNullOrEmpty(sentido) && periodo != null) {
                    datos = datos.Where(c => c.sentido == sentido && c.periodo.Year == periodo.Value.Year && c.periodo.Month == periodo.Value.Month);
                } else if (trafico != null && periodo != null) {
                    datos = datos.Where(c => c.periodo.Year == periodo.Value.Year && c.periodo.Month == periodo.Value.Month && trafico.Contains(c.trafico));
                } else if (!string.IsNullOrEmpty(sentido)) {
                    datos = datos.Where(c => c.sentido == sentido);
                } else if (trafico != null) {
                    datos = datos.Where(c => trafico.Contains(c.trafico));
                } else if (periodo != null) {
                    datos = datos.Where(c => c.periodo.Year == periodo.Value.Year && c.periodo.Month == periodo.Value.Month);
                }


                foreach (var elemento in datos) {

                    lista.Add(new
                    {
                        Id = elemento.Id,
                        sentido = elemento.sentido,
                        idSociedad = elemento.idSociedad,
                        sociedad = elemento.sociedad,
                        idTrafico = elemento.idTrafico,
                        trafico = elemento.trafico,
                        idServicio = elemento.idServicio,
                        servicio = elemento.servicio,
                        idDeudorAcreedor = elemento.idDeudorAcreedor,
                        deudorAcreedor = elemento.deudorAcreedor,
                        idOperador = elemento.idOperador,
                        operador = elemento.operador,
                        idGrupo = elemento.idGrupo,
                        grupo = elemento.grupo,
                        periodo = elemento.periodo.ToString("yyyy MMMM", new CultureInfo("es-ES")).ToUpper(),
                        importe = Convert.ToDecimal(elemento.importe),
                        moneda = elemento.moneda
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AgregarAjustesObjecion(string sentido, string sociedad, string trafico,
                                          string servicio, string deudorAcreedor, string operador,
                                          string grupo, DateTime periodo, decimal importe,
                                          string moneda, int lineaNegocio)
        {
            object respuesta = null;
            DateTime fecha_contable = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            try {
                ajustesObjecion abj = new ajustesObjecion();

                abj.sentido = sentido;
                abj.sociedad = sociedad;
                abj.trafico = trafico;
                abj.servicio = servicio;
                abj.deudorAcreedor = deudorAcreedor;
                abj.operador = operador;
                abj.grupo = grupo;
                abj.periodo = new DateTime(periodo.Year, periodo.Month, 1);
                abj.importe = importe;
                abj.moneda = moneda;
                abj.activo = 1;
                abj.lineaNegocio = lineaNegocio;
                abj.periodoContable = fecha_contable;
                db.ajustesObjecion.Add(abj);
                Log log = new Log();
                log.insertaNuevoOEliminado(abj, "Nuevo", "ajustesObjecion.html", Request.UserHostAddress);
                db.SaveChanges();
                respuesta = new { success = true, results = "ok" };
                db.spAjustesObjecion(1, lineaNegocio);
            } catch (Exception) {
                respuesta = new { success = false, results = "Hubo un error al momento de realizar la petición." };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public void ExportaCSV(int lineaNegocio, DateTime periodoContable, string sentido, List<string> trafico, DateTime? periodo)
        {
            try {

                StringWriter sw = new StringWriter();
                sw.WriteLine("Sentido;Sociedad;Trafico;Servicio;Deudor/Acreedor;IdOperador;Grupo;Periodo;Importe;Moneda");

                Response.ClearContent();
                Response.AddHeader("content-disposition", "attachment;filename=AjustesObjecion.csv");
                Response.ContentType = "text/csv";

                var datos = from oDatos in db.ajustesObjecion
                            where
                                oDatos.activo == 1 &&
                                oDatos.lineaNegocio == lineaNegocio &&
                                oDatos.periodoContable == periodoContable
                            select new
                            {
                                oDatos.Id,
                                oDatos.sentido,
                                oDatos.idSociedad,
                                oDatos.sociedad,
                                oDatos.idTrafico,
                                oDatos.trafico,
                                oDatos.idServicio,
                                oDatos.servicio,
                                oDatos.idDeudorAcreedor,
                                oDatos.deudorAcreedor,
                                oDatos.idOperador,
                                oDatos.operador,
                                oDatos.idGrupo,
                                oDatos.grupo,
                                oDatos.periodo,
                                oDatos.importe,
                                oDatos.moneda
                            };

                if (!string.IsNullOrEmpty(sentido) && trafico != null && periodo != null) {
                    datos = datos.Where(c => c.sentido == sentido && c.periodo.Year == periodo.Value.Year && c.periodo.Month == periodo.Value.Month && trafico.Contains(c.trafico));
                } else if (!string.IsNullOrEmpty(sentido) && trafico != null) {
                    datos = datos.Where(c => c.sentido == sentido && trafico.Contains(c.trafico));
                } else if (!string.IsNullOrEmpty(sentido) && periodo != null) {
                    datos = datos.Where(c => c.sentido == sentido && c.periodo.Year == periodo.Value.Year && c.periodo.Month == periodo.Value.Month);
                } else if (trafico != null && periodo != null) {
                    datos = datos.Where(c => c.periodo.Year == periodo.Value.Year && c.periodo.Month == periodo.Value.Month && trafico.Contains(c.trafico));
                } else if (!string.IsNullOrEmpty(sentido)) {
                    datos = datos.Where(c => c.sentido == sentido);
                } else if (trafico != null) {
                    datos = datos.Where(c => trafico.Contains(c.trafico));
                } else if (periodo != null) {
                    datos = datos.Where(c => c.periodo.Year == periodo.Value.Year && c.periodo.Month == periodo.Value.Month);
                }

                foreach (var elemento in datos) {

                    sw.WriteLine(string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}",

                        elemento.sentido.Replace("\r\n", string.Empty),
                        elemento.sociedad.Replace("\r\n", string.Empty),
                        elemento.trafico.Replace("\r\n", string.Empty),
                        elemento.servicio.Replace("\r\n", string.Empty),
                        elemento.deudorAcreedor.Replace("\r\n", string.Empty),
                        elemento.operador.Replace("\r\n", string.Empty),
                        elemento.grupo.Replace("\r\n", string.Empty),
                        elemento.periodo.ToString("dd/MM/yyyy"),
                        Convert.ToDouble(elemento.importe),
                        elemento.moneda.Replace("\r\n", string.Empty)));

                }

                Response.Write(sw.ToString());
                Response.End();
            } catch (Exception e) {
                var mensaje = e.Message;
            }
        }

        public JsonResult LlenaSociedad(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oSociedad in db.Sociedad
                            where oSociedad.Activo == 1 && oSociedad.Id_LineaNegocio == lineaNegocio
                            select new
                            {
                                oSociedad.Id,
                                oSociedad.NombreSociedad,
                                oSociedad.AbreviaturaSociedad,
                                oSociedad.Id_Sociedad
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        Id = elemento.Id,
                        NombreSociedad = elemento.NombreSociedad,
                        AbreviaturaSociedad = elemento.AbreviaturaSociedad,
                        Descripcion = elemento.Id_Sociedad + " - " + elemento.NombreSociedad
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaTrafico(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oTrafico in db.Trafico
                            where oTrafico.Activo == 1 && oTrafico.Id_LineaNegocio == lineaNegocio
                            select new
                            {
                                oTrafico.Id,
                                oTrafico.Descripcion,
                                oTrafico.Id_TraficoTR
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        Id = elemento.Id,
                        IdTraficoTR = elemento.Id_TraficoTR,
                        Descripcion = elemento.Id_TraficoTR + " - " + elemento.Descripcion
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaServicio(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oServicio in db.Servicio
                            where oServicio.Activo == 1 && oServicio.Id_LineaNegocio == lineaNegocio
                            select new
                            {
                                oServicio.Id,
                                oServicio.Id_Servicio,
                                oServicio.Servicio1
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        Id = elemento.Id,
                        Servicio = elemento.Servicio1,
                        Descripcion = elemento.Id_Servicio + " - " + elemento.Servicio1
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaDeudorAcreedor(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datosA = from oDeudor in db.Acreedor
                             where oDeudor.Activo == 1 && oDeudor.Id_LineaNegocio == lineaNegocio
                             select new
                             {
                                 oDeudor.Id,
                                 Deudor1 = oDeudor.Acreedor1,
                                 NombreDeudor = oDeudor.NombreAcreedor
                             };

                var datosD = from oDeudor in db.Deudor
                             where oDeudor.Activo == 1 && oDeudor.Id_LineaNegocio == lineaNegocio
                             select new
                             {
                                 oDeudor.Id,
                                 oDeudor.Deudor1,
                                 oDeudor.NombreDeudor
                             };
                var datos = Enumerable.Union(datosA, datosD);

                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        Id = elemento.Id,
                        DeudorAcreedor = elemento.Deudor1,
                        Descripcion = elemento.Deudor1 + " - " + elemento.NombreDeudor
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaOperador(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oOperador in db.Operador
                            where oOperador.Activo == 1 && oOperador.Id_LineaNegocio == lineaNegocio
                            select new
                            {
                                oOperador.Id,
                                oOperador.Id_Operador,
                                oOperador.Nombre
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        Id = elemento.Id,
                        IdOperador = elemento.Id_Operador,
                        Descripcion = elemento.Id_Operador + " - " + elemento.Nombre
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaGrupo(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oGrupo in db.Grupo
                            where oGrupo.Activo == 1 && oGrupo.Id_LineaNegocio == lineaNegocio
                            select new
                            {
                                oGrupo.Id,
                                oGrupo.Grupo1,
                                oGrupo.DescripcionGrupo
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        Id = elemento.Id,
                        Grupo = elemento.Grupo1,
                        Descripcion = elemento.Grupo1 + " - " + elemento.DescripcionGrupo

                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaMoneda(int lineaNegocio)
        {
            object respuesta = null;
            List<object> lista = new List<object>();
            try {
                var moneda = from oMoneda in db.Moneda
                             where oMoneda.Id_LineaNegocio == lineaNegocio
                             && oMoneda.Activo == 1
                             select new
                             {
                                 oMoneda.Id,
                                 oMoneda.Moneda1,
                                 oMoneda.Descripcion
                             };
                foreach (var elemento in moneda) {
                    lista.Add(new
                    {
                        id = elemento.Id,
                        id_moneda = elemento.Moneda1,
                        moneda = elemento.Descripcion
                    });
                }
                respuesta = new { success = true, results = lista };
            } catch (Exception e) {
                respuesta = new { success = false, results = e.Message };
            }
            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaTraficoFiltro(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oAjuste in db.ajustesObjecion
                            where oAjuste.activo == 1 && oAjuste.lineaNegocio == lineaNegocio
                            group oAjuste by oAjuste.trafico into g
                            select new
                            {
                                Id = g.Key,
                                trafico = g.Key
                            };

                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        Id = elemento.Id,
                        Trafico = elemento.trafico
                    });
                }

                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total = total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

    }
}