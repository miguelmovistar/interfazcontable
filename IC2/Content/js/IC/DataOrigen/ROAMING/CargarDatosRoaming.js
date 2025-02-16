﻿Ext.define('CMS.view.FileDownload', {
    extend: 'Ext.Component',
    alias: 'widget.FileDownloader',
    autoEl: {
        tag: 'iframe',
        cls: 'x-hidden',
        src: Ext.SSL_SECURE_URL
    },
    stateful: false,
    load: function (config) {
        var e = this.getEl();
        e.dom.src = config.url +
            (config.params ? '?' + Ext.urlEncode(config.params) : '');
        e.dom.onload = function () {
            if (e.dom.contentDocument.body.childNodes[0].wholeText == '404') {
                Ext.Msg.show({
                    title: 'NO FUE POSIBLE GENERAR EL DOCUMENTO...',
                    msg: 'Por favor contacte al area de soporte para identificar el origen del problema.',
                    buttons: Ext.Msg.OK,
                    icon: Ext.MessageBox.ERROR
                })
            }
        }
    }
});

Ext.Loader.setConfig({ enabled: true });
Ext.Loader.setPath('Ext.ux', '../ux');

var required = '<span style="color:red;font-weight:bold" data-qtip="Required">*</span>';

Ext.require([
    'Ext.form.*',
    'Ext.data.*',
    'Ext.grid.Panel',
    'Ext.selection.CheckboxModel',
    'Ext.layout.container.Column',
    'Ext.form.field.ComboBox',
    'Ext.window.MessageBox',
    'Ext.form.FieldSet',
    'Ext.tip.QuickTipManager',
    'Ext.toolbar.Paging',
    'Ext.ux.*'
]);

Ext.onReady(function () {
    Ext.QuickTips.init();
    var Body = Ext.getBody();
    var lineaNegocio = document.getElementById('idLinea').value;
    var tipoCarga;
    var ordenCarga;
    var idServicio;
    var documento;

    Ext.define('modeloDocumento',
        {
            extend: 'Ext.data.Model',
            fields: [
                { name: 'Id', mapping: 'Id' },
                { name: 'NombreArchivo', mapping: 'NombreArchivo' },
                { name: 'Ruta', mapping: 'Ruta' },
                { name: 'Periodo', mapping: 'Periodo' },
                { name: 'FechaCarga', mapping: 'FechaCarga' },
                { name: 'EstatusCarga', mapping: 'EstatusCarga' }
            ]
        });

    Ext.define('modeloDocumentoRoaming',
        {
            extend: 'Ext.data.Model',
            fields: [
                { name: 'Id', mapping: 'Id' },
                { name: 'nombreArchivo', mapping: 'nombreArchivo' },
                { name: 'ruta', mapping: 'ruta' },
                { name: 'periodo', mapping: 'periodo' },
                { name: 'fechaCarga', mapping: 'fechaCarga' },
                { name: 'tipoCarga', mapping: 'tipoCarga' },
                { name: 'ordenCarga', mapping: 'ordenCarga' },
                { name: 'fechaCarga', mapping: 'fechaCarga' }
            ]
        });

    Ext.define('modeloFecha',
        {
            extend: 'Ext.data.Model',
            fields: [
                { name: 'Id', mapping: 'Id' },
                //{ name: 'Id_Documento', mapping: 'Id_Documento' },
                { name: 'Periodo', mapping: 'Periodo' }
            ]
        });

    var storeLlenaFecha = Ext.create('Ext.data.Store', {
        model: 'modeloFecha',
        storeId: 'idstore_LlenaFecha',
        autoLoad: true,
        proxy: {
            type: 'ajax',
            url: '../' + VIRTUAL_DIRECTORY + 'CargarDatosRoaming/LlenaFecha?lineaNegocio=' + lineaNegocio,
            reader: {
                type: 'json',
                root: 'results',
                successProperty: 'success',
                totalProperty: 'total'
            },
            actionMethods: {
                create: 'POST', read: 'GET', update: 'POST', destroy: 'POST'
            }
        }
    });

    var panel = Ext.create('Ext.form.Panel', {
        frame: false,
        border: false,
        margin: '0 0 0 6',
        height: "70%",
        width: "100%",
        layout: { type: 'vbox' },
        flex: 1,
        items: [
            {
                html: "<div style='font-size:20px';>Cargar Datos Tráfico</div><br/>",
                width: '50%',
                border: false,
                bodyStyle: { "background-color": "#E6E6E6" },
            },
            {
                xtype: 'panel',
                layout: { type: 'hbox' },
                width: '100%',
                border: false,
                bodyStyle: { "background-color": "#E6E6E6" },
                items: [
                    {
                        xtype: 'combobox',
                        name: 'cmbFecha',
                        id: 'cmbFecha',
                        store: storeLlenaFecha,
                        queryMode: 'remote',
                        valueField: 'Periodo',
                        displayField: 'Periodo',
                        fieldLabel: "Fecha",
                        width: '25%',
                        margin: '5 0 0 55',
                        editable: false,
                        blankText: "El campo Fecha es requerido",
                        msgTarget: 'under',
                        maxLength: 100,
                        enforceMaxLength: true,
                        labelWidth: 40
                    },
                    {
                        xtype: 'button',
                        id: 'btnCargar',
                        margin: '0 0 0 50',
                        html: "<button class='btn btn-primary' style='outline:none; font-size: 15px'>Cargar Datos</button>",
                        border: false,
                        handler: function () {
                            var form = this.up('form').getForm();
                            if (form.wasValid) {
                                form.submit({
                                    url: '../' + VIRTUAL_DIRECTORY + 'CargarDatosRoaming/CargarDatos',
                                    waitMsg: "Cargando...",
                                    params:
                                    {
                                        Periodo: Ext.getCmp('cmbFecha').value,
                                        lineaNegocio: lineaNegocio
                                    },
                                    success: function (form, action) {
                                        Ext.Msg.show({
                                            title: "Confirmación",
                                            msg: "Datos cargados con éxito",
                                            buttons: Ext.Msg.OK,
                                            icon: Ext.MessageBox.INFO
                                        });
                                        Ext.getCmp('cmbFecha').value = "";
                                        Ext.getCmp('cmbFecha').clearValue;
                                        storeLlenaFecha.getProxy().extraParams.lineaNegocio = lineaNegocio;
                                        storeLlenaFecha.load();
                                       
                                        /*
                                        var data = Ext.JSON.decode(action.response.responseText);
                                        var string = "";

                                        if (action.result.procesados.length > 0) {
                                            string = "<b>Se han detectado los siguientes errores:</b><br><br>";
                                            for (var i = 0; i < action.result.procesados.length; ++i)
                                                string += "&nbsp &nbsp" + action.result.procesados[i] + "<br>";
                                        } else {
                                            string = "<b>Todos los registros se han cargado correctamente.</b>";
                                        }
                                        
                                        var algo = Ext.create('Ext.form.Panel', {
                                            title: 'Resultados de la carga',
                                            width: '50%',
                                            margin: '0 10 0 10',
                                            border: false,
                                            items: [
                                                {
                                                    xtype: 'fieldset',
                                                    border: true,
                                                    layout: { type: 'hbox' },
                                                    margin: '0 0 0 0',
                                                    width: "20%",
                                                    items: [
                                                        {
                                                            html: "<div><h6 style='color:blue'><b>Registros cargados con éxito</b></h6></div>",
                                                            width: "30%",
                                                            border: false
                                                        }, {
                                                            html: "<h6><b>" + action.result.exitos +"</b></h6>",
                                                            width: "15%",
                                                            border: false,
                                                        }
                                                    ]
                                                },
                                                {
                                                    xtype: 'fieldset',
                                                    border: true,
                                                    layout: { type: 'hbox' },
                                                    margin: '0 0 0 0',
                                                    width: "10%",
                                                    items: [
                                                        {
                                                            html: "<div><h6 style='color:blue'><b>Registros procesados<b></h6></div>",
                                                            width: "30%",
                                                            border: false,
                                                        }, {
                                                            html: "<h6 style:'overflow: auto;'><b>" + action.result.total +"<b></h6>",
                                                            width: "15%",
                                                            border: false,
                                                        }
                                                    ]
                                                },
                                                {
                                                    xtype: 'fieldset',
                                                    border: true,
                                                    layout: { type: 'hbox' },
                                                    margin: '0 0 0 0',
                                                    width: "20%",
                                                    id: 'registrosErroneos',
                                                    flex:1,
                                                    items: [
                                                        {
                                                            html: "<div><h6 style='color:blue'><b>Registros erróneos<b></h6></div>",
                                                            width: "30%",
                                                            border: false,
                                                        }, {
                                                            html: "<div style='overflow: auto; height: 200px; font-size:11.5px'>" + string + "</div>",
                                                            width: "40%",
                                                            height: "100",
                                                            overflow: 'auto',
                                                            border: false
                                                        }
                                                    ]
                                                }
                                            ],
                                            renderTo: Body
                                        });

                                        Ext.EventManager.onWindowResize(function (w, h) {
                                            algo.setSize(w - 20, h - 355);
                                            algo.doComponentLayout();
                                        });

                                        Ext.EventManager.onDocumentReady(function (w, h) {
                                            algo.setSize(Ext.getBody().getViewSize().width - 20, Ext.getBody().getViewSize().height - 355);
                                            algo.doComponentLayout();
                                        });
                                        */
                                    },
                                    failure: function (forms, action) {
                                        var data = Ext.JSON.decode(action.response.responseText);
                                        Ext.Msg.show({
                                            title: "Notificación",
                                            msg: action.result.results,
                                            buttons: Ext.Msg.OK,
                                            icon: Ext.MessageBox.INFO
                                        });
                                    }
                                });
                            }
                        }
                    }
                ]
            }
        ],
        renderTo: Body,
        bodyStyle: { "background-color": "#E6E6E6" },
    });

    Ext.EventManager.onWindowResize(function (w, h) {
        panel.setSize(w - 50, h - 550);
        panel.doComponentLayout();
    });

    Ext.EventManager.onDocumentReady(function (w, h) {
        panel.setSize(Ext.getBody().getViewSize().width - 50, Ext.getBody().getViewSize().height - 550);
        panel.doComponentLayout();
    });
});