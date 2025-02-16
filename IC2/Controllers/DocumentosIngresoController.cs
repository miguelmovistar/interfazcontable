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
    public class DocumentosIngresoController : Controller
    {
        ICPruebaEntities db = new ICPruebaEntities();
        // GET: DocumentosIngreso
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

        public JsonResult LlenaGrid(int? lineaNegocio, DateTime fechaInicial, DateTime fechaFinal, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;

            int total;
            try {
                var datos = from oDatos in db.documentosIngreso
                            where oDatos.activo == 1 &&
                            oDatos.lineaNegocio == lineaNegocio &&
                            oDatos.fechaContable >= fechaInicial && oDatos.fechaContable <= fechaFinal
                            select new
                            {
                                oDatos.Id,
                                oDatos.ano,
                                oDatos.fechaContable,
                                oDatos.fechaConsumo,
                                oDatos.idSociedad,
                                oDatos.compania,
                                oDatos.idServicio,
                                oDatos.servicio,
                                oDatos.idGrupo,
                                oDatos.grupo,
                                oDatos.idDeudor,
                                oDatos.deudor,
                                oDatos.idOperador,
                                oDatos.operador,
                                oDatos.nombreOperador,
                                oDatos.codigoMaterial,
                                oDatos.idTrafico,
                                oDatos.trafico,
                                oDatos.montoIva,
                                oDatos.iva,
                                oDatos.idMoneda,
                                oDatos.moneda,
                                oDatos.minutos,
                                oDatos.tarifa,
                                oDatos.monto,
                                oDatos.montoFacturado,
                                oDatos.fechaFactura,
                                oDatos.factura,
                                oDatos.tipoCambio,
                                oDatos.montoMXP,
                                oDatos.idCuentaResultado,
                                oDatos.cuentaContable,
                                oDatos.claseDocumento,
                                oDatos.claseDocumentoSAP,
                                oDatos.numDocumentoPF
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        elemento.Id,
                        ano = elemento.ano.Year,
                        fechaContable = elemento.fechaContable.Day.ToString("00") + "-"+ elemento.fechaContable.Month.ToString("00") + "-" + + elemento.fechaContable.Year,
                        fechaContableR = elemento.fechaContable.Day.ToString("00") + "-" + elemento.fechaContable.Month.ToString("00") + "-" + elemento.fechaContable.Year,
                        fechaConsumo = elemento.fechaConsumo.Day.ToString("00") + "-" + elemento.fechaConsumo.Month.ToString("00") + "-" + elemento.fechaConsumo.Year,
                        fechaConsumoR = elemento.fechaConsumo.Day.ToString("00") + "-" + elemento.fechaConsumo.Month.ToString("00") + "-" + elemento.fechaConsumo.Year,
                        elemento.idSociedad,
                        elemento.compania,
                        elemento.idServicio,
                        elemento.servicio,
                        elemento.idGrupo,
                        elemento.grupo,
                        elemento.idDeudor,
                        elemento.deudor,
                        elemento.idOperador,
                        elemento.operador,
                        elemento.nombreOperador,
                        elemento.codigoMaterial,
                        elemento.idTrafico,
                        elemento.trafico,
                        elemento.montoIva,
                        elemento.iva,
                        elemento.idMoneda,
                        elemento.moneda,
                        elemento.minutos,
                        elemento.tarifa,
                        elemento.monto,
                        elemento.montoFacturado,
                        fechaFactura = elemento.fechaFactura.Day.ToString("00") + "-" + elemento.fechaFactura.Month.ToString("00") + "-" + + elemento.fechaFactura.Year,
                        fechaFacturaR = elemento.fechaFactura.Day.ToString("00") + "-" + elemento.fechaFactura.Month.ToString("00") + "-" + elemento.fechaFactura.Year,
                        elemento.factura,
                        elemento.tipoCambio,
                        elemento.montoMXP,
                        elemento.idCuentaResultado,
                        elemento.cuentaContable,
                        elemento.claseDocumento,
                        elemento.claseDocumentoSAP,
                        elemento.numDocumentoPF
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CargarCSV(HttpPostedFileBase archivoCSV, int lineaNegocio)
        {
            List<string> listaErrores = new List<string>();
            var hoy = DateTime.Now;
            IEnumerable<string> lineas = null;
            object respuesta = null;
            int totalProcesados = 0;
            int lineaActual = 1;
            bool status = false;    
            string ope, fact;
            string exception = "Error, se presento un error inesperado.";

            try {
                /// Se salta la primera linea, el encabezado
                List<string> csvData = new List<string>();
                using (System.IO.StreamReader reader = new System.IO.StreamReader(archivoCSV.InputStream, Encoding.Default)) {
                    while (!reader.EndOfStream) {
                        csvData.Add(reader.ReadLine());
                    }
                }

                lineas = csvData.Skip(1);

                //lineas = System.IO.File.ReadLines(path).Skip(1);
                using (TransactionScope scope = new TransactionScope()) {
                    foreach (string ln in lineas) {
                        string linea = ln.Replace('%', ' ');
                        var lineaSplit = SepararLineas(linea);
                        ++lineaActual;
                        if (lineaSplit.Count() == 25) {
                            documentosIngreso documento = new documentosIngreso();
                            try {
                                ope = lineaSplit[6];
                                fact = lineaSplit[19];
                                
                                //var veriDocumento = db.documentosIngreso.Where(x => x.operador == ope && x.factura == fact && x.activo == 1 && x.lineaNegocio == lineaNegocio).SingleOrDefault();
                                documentosIngreso veriDocumento = db.documentosIngreso.Where(x => x.operador == ope && x.factura == fact && x.activo == 1 && x.lineaNegocio == lineaNegocio).FirstOrDefault();
                                if (veriDocumento != null) {
                                    listaErrores.Add("Línea " + lineaActual + ": El Operador y Factura actual ya estan dados de alta.");
                                    continue;
                                }

                                if (lineaSplit[0] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Año es obligatorio.");
                                    continue;
                                }
                                string strDate = lineaSplit[0] + "-01-01";
                                documento.ano = DateTime.Parse(strDate);

                                if (lineaSplit[1] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Fecha Contable es obligatorio.");
                                    continue;
                                }

                                DateTime dtFecha = ConvierteFecha(lineaSplit[1], char.Parse("-"), "DMY");

                                documento.fechaContable = dtFecha;//DateTime.ParseExact( lineaSplit[1], "dd-MM-yyyy", new CultureInfo("en-US"), DateTimeStyles.None);

                                if (lineaSplit[2] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Fecha Consumo es obligatorio.");
                                    continue;
                                }

                                dtFecha = ConvierteFecha("01-" + lineaSplit[2], char.Parse("-"), "DMY");

                                documento.fechaConsumo = dtFecha; //DateTime.ParseExact( lineaSplit[2], "dd/MM/yyyy", new CultureInfo("en-US"), DateTimeStyles.None);
                                if (documento.fechaConsumo.Month >= hoy.Month && documento.fechaConsumo.Year == hoy.Year) {
                                    listaErrores.Add("Línea " + lineaActual + ": No se permite cargar facturas con Mes Consumo que sean del mes en curso en adelante.");
                                    continue;
                                }

                                if (lineaSplit[3] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Compañia es obligatorio.");
                                    continue;
                                }
                                documento.compania = lineaSplit[3];
                                documento.servicio = lineaSplit[4];

                                if (lineaSplit[5] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Grupo es obligatorio.");
                                    continue;
                                }
                                documento.grupo = lineaSplit[5];

                                if (lineaSplit[6] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Id Operador es obligatorio.");
                                    continue;
                                }
                                documento.operador = lineaSplit[6];
                                documento.nombreOperador = lineaSplit[7];

                                if (lineaSplit[8] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Deudor es obligatorio.");
                                    continue;
                                }
                                documento.deudor = lineaSplit[8];

                                if (lineaSplit[9] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Código Material es obligatorio.");
                                    continue;
                                }
                                documento.codigoMaterial = lineaSplit[9];
                                documento.trafico = lineaSplit[10];

                                if (lineaSplit[11] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Monto IVA es obligatorio.");
                                    continue;
                                }
                                documento.montoIva = decimal.Parse(lineaSplit[11]);

                                if (lineaSplit[12] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo IVA es obligatorio.");
                                    continue;
                                }
                                documento.iva = decimal.Parse(lineaSplit[12]);

                                if (lineaSplit[13] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Moneda es obligatorio.");
                                    continue;
                                }
                                documento.moneda = lineaSplit[13];

                                if (lineaSplit[14] == null || lineaSplit[14] == "")
                                    documento.minutos = null;
                                else
                                    documento.minutos = decimal.Parse(lineaSplit[14]);

                                if(lineaSplit[15] == null || lineaSplit[15] == "")
                                    documento.tarifa = null;
                                else
                                    documento.tarifa = decimal.Parse(lineaSplit[15]);

                                if (lineaSplit[16] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Monto es obligatorio.");
                                    continue;
                                }
                                documento.monto = decimal.Parse(lineaSplit[16]);

                                if (lineaSplit[17] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Monto Facturado es obligatorio.");
                                    continue;
                                }
                                documento.montoFacturado = decimal.Parse(lineaSplit[17]);

                                if (lineaSplit[18] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Fecha Factura es obligatorio.");
                                    continue;
                                }

                                dtFecha = ConvierteFecha(lineaSplit[18], char.Parse("-"), "DMY");

                                documento.fechaFactura = dtFecha; //DateTime.ParseExact(lineaSplit[18], "dd/MM/yyyy", new CultureInfo("en-US"), DateTimeStyles.None);// DateTime.Parse(lineaSplit[18]);

                                if (lineaSplit[19] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Factura es obligatorio.");
                                    continue;
                                }
                                documento.factura = lineaSplit[19];

                                if (lineaSplit[20] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Tipo Cambio es obligatorio.");
                                    continue;
                                }
                                documento.tipoCambio = decimal.Parse(lineaSplit[20]);

                                if (lineaSplit[21] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Monto MXP es obligatorio.");
                                    continue;
                                }
                                documento.montoMXP = decimal.Parse(lineaSplit[21]);
                                documento.cuentaContable = lineaSplit[22];
                                
                                var result = db.spValidaDocumentosIng( documento.compania, documento.servicio,
                                    documento.grupo, documento.deudor, documento.operador, documento.trafico,
                                    documento.moneda, documento.cuentaContable, documento.codigoMaterial, lineaNegocio
                                ).ToList();

                                documento.idSociedad = result[0].idSociedad;
                                documento.idServicio = result[0].idServicio;
                                documento.idGrupo = result[0].idGrupo;
                                documento.idOperador = result[0].idOperador;
                                documento.idDeudor = result[0].idDeudor;
                                documento.idTrafico = result[0].idTrafico;
                                documento.idMoneda = result[0].idMoneda;
                                documento.idCuentaResultado = result[0].idCuentaResultado;

                                if (result[0].idStatus == 1) {
                                    documento.activo = 1;
                                    totalProcesados++;
                                } else {
                                    documento.activo = 0;
                                    var cadena = result[0].cadenaResultado;
                                    listaErrores.Add("Línea " + lineaActual + ": Error en la carga, no se encontraron coincidencias" +
                                        " en los siguientes catálogo(s)" + cadena.Remove(cadena.Length - 1) + ".");
                                }

                                if (lineaSplit[23] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Clase Documento es obligatorio.");
                                    continue;
                                }
                                documento.claseDocumento = lineaSplit[23];

                                if (lineaSplit[24] == "") {
                                    listaErrores.Add("Línea " + lineaActual + ": El campo Clase Documento SAP es obligatorio.");
                                    continue;
                                }
                                documento.claseDocumentoSAP = lineaSplit[24];
                                documento.estatus = "PENDIENTE DE PROCESAR";
                                documento.lineaNegocio = lineaNegocio;
                                documento.tipoCarga = "MASIVA";
                                db.documentosIngreso.Add(documento);
                                Log log = new Log();
                                log.insertaNuevoOEliminado(documento, "Nuevo", "documentosIngreso.html", Request.UserHostAddress);

                            } catch (FormatException ) {
                                listaErrores.Add("Línea " + lineaActual + ": Campo con formato erróneo.");
                            } catch (Exception ) {
                                listaErrores.Add("Línea " + lineaActual + ": Error desconocido. ");
                            }
                        } else {
                            listaErrores.Add("Línea " + lineaActual + ": Número de campos insuficiente.");
                        }
                    }
                    db.SaveChanges();
                    scope.Complete();
                    exception = "Datos cargados con éxito";
                    status = true;
                }
            } catch (FileNotFoundException ) {
                exception = "El archivo Selecionado aún no existe en el Repositorio.";
                status = false;
            } catch (UnauthorizedAccessException ) {
                exception = "No tiene permiso para acceder al archivo actual.";
                status = false;
            } catch (IOException e) when ((e.HResult & 0x0000FFFF) == 32) {
                exception = "Falta el nombre del archivo, o el archivo o directorio está en uso.";
                status = false;
            } catch (TransactionAbortedException ) {
                exception = "Transacción abortada. Se presentó un error.";
                status = false;
            } catch (Exception err) {
                exception = "Error desconocido. " + err.InnerException.ToString();
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

        public JsonResult AgregarDocumentosIngreso(int ano, DateTime fechaContable, DateTime fechaConsumo, int idSociedad, 
            string compania, int? idServicio, string servicio, int idGrupo, string grupo, int idDeudor, string deudor, int idOperador,
            string operador, string nombreOperador, string codigoMaterial, int? idTrafico, string trafico, decimal montoIva, 
            decimal iva, int idMoneda, string moneda, decimal? minutos, decimal? tarifa, decimal monto, decimal montoFacturado, 
            DateTime fechaFactura, string factura, decimal tipoCambio, decimal montoMXP, int? idCuentaResultado, string cuentaContable, 
            string claseDocumento, string claseDocumentoSAP, string numDocumentoPF, int lineaNegocio)
        {
            object respuesta = null;

            documentosIngreso veriDocumento = db.documentosIngreso.Where(x => x.operador == operador && x.factura == factura && x.activo == 1 && x.lineaNegocio == lineaNegocio).SingleOrDefault();
            if (veriDocumento == null) {
                try {
                    documentosIngreso documento = new documentosIngreso
                    {
                        ano = new DateTime(ano, 01, 01),
                        fechaContable = fechaContable,
                        fechaConsumo = fechaConsumo,
                        idSociedad = idSociedad,
                        compania = compania,
                        idServicio = idServicio,
                        servicio = servicio,
                        idGrupo = idGrupo,
                        grupo = grupo,
                        idDeudor = idDeudor,
                        deudor = deudor,
                        idOperador = idOperador,
                        operador = operador,
                        nombreOperador = nombreOperador,
                        codigoMaterial = codigoMaterial,
                        idTrafico = idTrafico,
                        trafico = trafico,
                        montoIva = montoIva,
                        iva = iva,
                        idMoneda = idMoneda,
                        moneda = moneda,
                        minutos = minutos,
                        tarifa = tarifa,
                        monto = monto,
                        montoFacturado = montoFacturado,
                        fechaFactura = fechaFactura,
                        factura = factura,
                        tipoCambio = tipoCambio,
                        montoMXP = montoMXP,
                        idCuentaResultado = idCuentaResultado,
                        cuentaContable = cuentaContable,
                        claseDocumento = claseDocumento,
                        claseDocumentoSAP = claseDocumentoSAP,
                        numDocumentoPF = numDocumentoPF,
                        activo = 1,
                        lineaNegocio = lineaNegocio,
                        estatus = "PENDIENTE DE PROCESAR",
                        tipoCarga = "MANUAL"
                    };
                    db.documentosIngreso.Add(documento);
                    Log log = new Log();
                    log.insertaNuevoOEliminado(documento, "Nuevo", "documentosIngreso.html", Request.UserHostAddress);

                    db.SaveChanges();
                    respuesta = new { success = true, results = "ok" };
                } catch (Exception ) {
                    respuesta = new { success = false, results = "Hubo un error al momento de realizar la petición." };
                }
            } else {
                respuesta = new { success = false, results = "El Operador y Factura actual ya estan dados de alta." };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ModificarDocumentosIngreso(int lineaNegocio, int id, int ano, DateTime fechaContable, DateTime fechaConsumo,
            int idSociedad, string compania, int? idServicio, string servicio, int idGrupo, string grupo, int idDeudor, string deudor,
            int idOperador, string operador, string nombreOperador, string codigoMaterial, int? idTrafico, string trafico, 
            decimal montoIva, decimal iva, int idMoneda, string moneda, decimal? minutos, decimal? tarifa, decimal monto, 
            decimal montoFacturado, DateTime fechaFactura, string factura, decimal tipoCambio, decimal montoMXP, 
            int? idCuentaResultado, string cuentaContable, string claseDocumento, string claseDocumentoSAP, string numDocumentoPF)
        {
            object respuesta = null;

            documentosIngreso VerificaDoc = db.documentosIngreso.Where(x => x.operador == operador && x.factura == factura && x.activo == 1 && x.lineaNegocio == lineaNegocio).SingleOrDefault();
            if (VerificaDoc == null || VerificaDoc.Id == id) {
                try {
                    documentosIngreso documento = db.documentosIngreso.Where(x => x.Id == id).SingleOrDefault();

                    //if (documento.lineaNegocio != 3) {
                    //    documento.idMoneda = idMoneda;
                    //    documento.moneda = moneda;
                    //}

                    documento.ano = new DateTime(ano, 01, 01);
                    documento.fechaContable = fechaContable;
                    documento.fechaConsumo = fechaConsumo;
                    documento.idSociedad = idSociedad;
                    documento.compania = compania;
                    documento.idServicio = idServicio;
                    documento.servicio = servicio;
                    documento.idGrupo = idGrupo;
                    documento.grupo = grupo;
                    documento.idDeudor = idDeudor;
                    documento.deudor = deudor;
                    documento.idOperador = idOperador;
                    documento.operador = operador;
                    documento.nombreOperador = nombreOperador;
                    documento.codigoMaterial = codigoMaterial;
                    documento.idTrafico = idTrafico;
                    documento.trafico = trafico;
                    documento.montoIva = montoIva;
                    documento.iva = iva;
                    documento.idMoneda = idMoneda;
                    documento.moneda = moneda;
                    documento.minutos = minutos;
                    documento.tarifa = tarifa;
                    documento.monto = monto;
                    documento.montoFacturado = montoFacturado;
                    documento.fechaFactura = fechaFactura;
                    documento.factura = factura;
                    documento.tipoCambio = tipoCambio;
                    documento.montoMXP = montoMXP;
                    documento.idCuentaResultado = idCuentaResultado;
                    documento.cuentaContable = cuentaContable;
                    documento.claseDocumento = claseDocumento;
                    documento.claseDocumentoSAP = claseDocumentoSAP;
                    documento.numDocumentoPF = numDocumentoPF;

                    Log log = new Log();
                    log.insertaBitacoraModificacion(documento, "Id", documento.Id, "documentosIngreso.html", Request.UserHostAddress);

                    db.SaveChanges();
                    respuesta = new { success = true, results = "ok" };
                } catch (Exception ) {
                    respuesta = new { success = false, results = "Hubo un error mientras se procesaba la petición." };
                }
            } else {
                respuesta = new { success = false, results = "El Operador y Factura actual ya estan dados de alta." };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult BorrarDocumentosIngreso(string strID)
        {
            object respuesta = null;
            int Id = 0;
            strID = strID.TrimEnd(',');
            try {
                string[] Ids = strID.Split(',');
                for (int i = 0; i < Ids.Length; i++) {
                    if (Ids[i].Length != 0) {
                        Id = int.Parse(Ids[i]);
                        documentosIngreso documento = db.documentosIngreso.Where(x => x.Id == Id && x.activo == 1).SingleOrDefault();
                        documento.activo = 0;
                        Log log = new Log();
                        log.insertaNuevoOEliminado(documento, "Eliminado", "documentosIngreso.html", Request.UserHostAddress);

                        db.SaveChanges();
                    }
                }
                respuesta = new { success = true, result = "ok" };
            } catch (Exception ex) {
                respuesta = new { success = false, result = ex.Message };
            }
            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaDeudor(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oDeudor in db.Deudor
                            where oDeudor.Activo == 1 && oDeudor.Id_LineaNegocio == lineaNegocio
                            select new
                            {
                                oDeudor.Id,
                                oDeudor.Deudor1,
                                oDeudor.NombreDeudor
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        elemento.Id,
                        Deudor = elemento.Deudor1,
                        Descripcion = elemento.Deudor1 + " - " + elemento.NombreDeudor
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaCompania(int lineaNegocio, int start, int limit)
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
                                oSociedad.Id_Sociedad,
                                oSociedad.AbreviaturaSociedad
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        elemento.Id,
                        Nombre = elemento.AbreviaturaSociedad,
                        Descripcion = elemento.Id_Sociedad + " - " + elemento.AbreviaturaSociedad
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
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
                var datos = from oSservicio in db.Servicio
                            where oSservicio.Activo == 1 && oSservicio.Id_LineaNegocio == lineaNegocio
                            select new
                            {
                                oSservicio.Id,
                                oSservicio.Servicio1,
                                oSservicio.Id_Servicio
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        elemento.Id,
                        Servicio = elemento.Servicio1,
                        Descripcion = elemento.Id_Servicio + " - " + elemento.Servicio1
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
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
                        elemento.Id,
                        Grupo = elemento.Grupo1,
                        Descripcion = elemento.Grupo1 + " - " + elemento.DescripcionGrupo
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
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
                        elemento.Id,
                        Operador = elemento.Id_Operador,
                        Descripcion = elemento.Id_Operador + " - " + elemento.Nombre
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaCodigoMaterial(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oCuenta in db.CuentaResultado
                            where oCuenta.Activo == 1 &&
                            oCuenta.Id_LineaNegocio == lineaNegocio &&
                            oCuenta.Sentido == "Ingresos"
                            select new
                            {
                                oCuenta.Id,
                                oCuenta.Material,
                                oCuenta.Codigo_Material
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        elemento.Id,
                        Codigo = elemento.Codigo_Material,
                        Descripcion = elemento.Codigo_Material + " - " + elemento.Material
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
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
                        elemento.Id,
                        Desc = elemento.Id_TraficoTR,
                        Descripcion = elemento.Id_TraficoTR + " - " + elemento.Descripcion
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaMoneda(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oMoneda in db.Moneda
                            where oMoneda.Activo == 1 && oMoneda.Id_LineaNegocio == lineaNegocio
                            select new
                            {
                                oMoneda.Id,
                                oMoneda.Moneda1,
                                oMoneda.Descripcion
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        elemento.Id,
                        Moneda = elemento.Moneda1,
                        Descripcion = elemento.Moneda1 + " - " + elemento.Descripcion
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LlenaCuenta(int lineaNegocio, int start, int limit)
        {
            List<object> lista = new List<object>();
            object respuesta = null;
            int total;

            try {
                var datos = from oCuenta in db.CuentaResultado
                            where oCuenta.Activo == 1 && oCuenta.Id_LineaNegocio == lineaNegocio
                            && oCuenta.Sentido == "Costos"
                            select new
                            {
                                oCuenta.Id,
                                oCuenta.Cuenta
                            };
                foreach (var elemento in datos) {
                    lista.Add(new
                    {
                        elemento.Id,
                        elemento.Cuenta
                    });
                }
                total = lista.Count();
                lista = lista.Skip(start).Take(limit).ToList();
                respuesta = new { success = true, results = lista, total };
            } catch (Exception e) {
                lista = null;
                respuesta = new { success = false, results = e.Message };
            }

            return Json(respuesta, JsonRequestBehavior.AllowGet);
        }

        public string[] SepararLineas(string cadena)
        {
            var aux = "";
            int i;
            for (i = 0; i < cadena.Length - 1; ++i) {
                if (cadena[i] == '"') {
                    do {
                        aux += cadena[i];
                        ++i;
                    } while (cadena[i] != '"');
                    aux += cadena[i];
                }
                if (cadena[i] == ';')
                    aux += '|';
                else
                    aux += cadena[i];
            }

            aux += cadena[i];

            return aux.Split('|');
        }

        public DateTime ConvierteFecha (string strFecha, char chrSepara, string strFormat )
        {
            string strDia = "";
            string strMes = "";
            string strAnio = "";
            DateTime dtData = DateTime.Parse("2000-01-01");

            string[] arrStrFeha = strFecha.Split(chrSepara);

            if (arrStrFeha != null)
            {
                if (strFormat == "DMY")
                {
                    strDia = arrStrFeha[0];
                    strMes = arrStrFeha[1];
                    strAnio = arrStrFeha[2];
                }
                else if (strFormat == "YMD")
                {
                    strDia = arrStrFeha[2];
                    strMes = arrStrFeha[1];
                    strAnio = arrStrFeha[0];
                }
                else if (strFormat == "MYD")
                {
                    strDia = arrStrFeha[0];
                    strMes = arrStrFeha[2];
                    strAnio = arrStrFeha[1];
                }

                if (strMes.Length <= 2)
                {
                    bool blesNumero = int.TryParse(strMes, out int n);

                    if (blesNumero == true)
                    {
                        if (strMes.Length == 1)
                        {
                            strMes = '0' + strMes;
                        }
                    }
                }
                else
                {
                    switch(strMes.ToUpper()){
                        case "ENE": case "ENERO": case "JAN": case "JANUARY":
                            strMes = "01";
                            break;

                        case "FEB": case "FEBRERO": case "FEBRUARY":
                            strMes = "02";
                            break;

                        case "MAR": case "MARZO": case "MARCH":
                            strMes = "03";
                            break;

                        case "ABR": case "ABRIL": case "APR": case "APRIL":
                            strMes = "04";
                            break;
                        
                        case "MAY": case "MAYO":
                            strMes = "05";
                            break;

                        case "JUN": case "JUNIO": case "JUNE":
                            strMes = "06";
                            break;

                        case "JUL":  case "JULIO": case "JULY": 
                            strMes = "07";
                            break;

                        case "AGO": case "AGOSTO": case "AUG": case "AUGUST":
                            strMes = "08";
                            break;

                        case "SEP": case "SEPTIEMBRE": case "SEPTEMBER":
                            strMes = "09";
                            break;

                        case "OCT": case "OCTUBRE": case "OCTOBER":
                            strMes = "10";
                            break;

                        case "NOV": case "NOVIEMBRE": case "NOVEMBER":
                            strMes = "11";
                            break;

                        case "DIC": case "DICIEMBRE": case "DEC": case "DECEMBER":
                            strMes = "12";
                            break;
                        default:
                            strMes = "01";
                            strAnio = "01";
                            break;
                    }
                }

                if (strAnio.Length == 2)
                {
                    strAnio = "20" + strAnio;
                }
                strFecha = strAnio + "-" + strMes + "-" + strDia;
                dtData = DateTime.Parse(strFecha);
            }
            
            return dtData;
        }

    }
}