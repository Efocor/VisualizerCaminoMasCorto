using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace VisualizadorCaminoMasCorto
{
    public partial class FormPrincipal : Form
    {
        private List<Nodo> nodos;
        private List<Arista> aristas;
        private Nodo nodoInicial;
        private Nodo nodoFinal;
        private bool agregandoNodo;
        private bool agregandoArista;
        private bool seleccionandoInicio;
        private bool seleccionandoFin;
        private Nodo nodoTemp;
        private const int RadioNodo = 20;

        public FormPrincipal()
        {
            InitializeComponentes();
            InicializarVariables();
            ConfigurarEventos();
        }

        private void InitializeComponentes()
        {
            this.Size = new Size(800, 600);
            this.Text = "CaminoMásCortoVisión";

            //panel para dibujar sin parpadeos y buen rendimiento
            panelDibujo = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            // Habilita DoubleBuffering mediante reflexión para mejor rendimiento
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(panelDibujo, true, null);

            panelControl = new Panel
            {
                Dock = DockStyle.Right,
                Width = 200,
                BackColor = Color.LightGray
            };

            btnAgregarNodo = new Button
            {
                Text = "Agregar Nodo",
                Location = new Point(10, 10),
                Width = 180
            };

            btnAgregarArista = new Button
            {
                Text = "Agregar Arista",
                Location = new Point(10, 40),
                Width = 180
            };

            btnSeleccionarInicio = new Button
            {
                Text = "Seleccionar Inicio",
                Location = new Point(10, 70),
                Width = 180
            };

            btnSeleccionarFin = new Button
            {
                Text = "Seleccionar Fin",
                Location = new Point(10, 100),
                Width = 180
            };

            btnEncontrarCamino = new Button
            {
                Text = "Encontrar Camino",
                Location = new Point(10, 130),
                Width = 180
            };

            btnLimpiar = new Button
            {
                Text = "Limpiar Todo",
                Location = new Point(10, 160),
                Width = 180
            };

            panelControl.Controls.AddRange(new Control[] {
                btnAgregarNodo, btnAgregarArista, btnSeleccionarInicio,
                btnSeleccionarFin, btnEncontrarCamino, btnLimpiar
            });

            this.Controls.AddRange(new Control[] { panelDibujo, panelControl });
        }

        private void InicializarVariables()
        {
            nodos = new List<Nodo>();
            aristas = new List<Arista>();
            agregandoNodo = false;
            agregandoArista = false;
            seleccionandoInicio = false;
            seleccionandoFin = false;
        }

        //Mouseclick más simple y sin stuttering o problemas en el dibujo
        private void PanelDibujo_MouseClick(object sender, MouseEventArgs e)
        {
            if (agregandoNodo)
            {
                AgregarNodo(e.Location);
            }
            else if (agregandoArista)
            {
                ProcesarClickArista(e.Location);
            }
            else if (seleccionandoInicio || seleccionandoFin)
            {
                ProcesarSeleccionNodo(e.Location);
            }
        }

        //.....Se agrega variable para tracking del mouse
        private Point mousePosicion = Point.Empty;

        //.....Evento MouseMove
        private void PanelDibujo_MouseMove(object sender, MouseEventArgs e)
        {
            mousePosicion = e.Location;
            if (agregandoArista && nodoTemp != null)
            {
                //....Si se está agregando una arista y hay un nodo temporal, forzar el repintado
                panelDibujo.Invalidate();
            }
            else
            {

                //....Verifica si el mouse está cerca de algún nodo para mostrar hint
                bool cercaDeNodo = nodos.Any(n =>
                    Math.Sqrt(Math.Pow(n.Posicion.X - e.X, 2) +
                    Math.Pow(n.Posicion.Y - e.Y, 2)) < RadioNodo * 1.5);
                    Cursor = Cursors.Hand;

                if (cercaDeNodo)
                {
                    //....Si el mouse está cerca de un nodo, forzar el repintado
                    panelDibujo.Invalidate();
                }
            }
        }

        private void ConfigurarEventos()
        {
            panelDibujo.Paint += PanelDibujo_Paint;
            panelDibujo.MouseClick += PanelDibujo_MouseClick;
            panelDibujo.MouseMove += PanelDibujo_MouseMove;

            btnAgregarNodo.Click += (s, e) => {
                DesactivarTodosModos();
                agregandoNodo = true;
            };

            btnAgregarArista.Click += (s, e) => {
                DesactivarTodosModos();
                agregandoArista = true;
            };

            btnSeleccionarInicio.Click += (s, e) => {
                DesactivarTodosModos();
                seleccionandoInicio = true;
            };

            btnSeleccionarFin.Click += (s, e) => {
                DesactivarTodosModos();
                seleccionandoFin = true;
            };

            btnEncontrarCamino.Click += (s, e) => {
                EncontrarYMostrarCamino();
            };

            btnLimpiar.Click += (s, e) => {
                LimpiarTodo();
            };
        }

        private void DesactivarTodosModos()
        {
            agregandoNodo = false;
            agregandoArista = false;
            seleccionandoInicio = false;
            seleccionandoFin = false;
            nodoTemp = null;
        }
        private void AgregarNodo(Point ubicacion)
        {
            if (!ExisteNodoEnPosicion(ubicacion))
            {
                var nuevoNodo = new Nodo
                {
                    Id = nodos.Count,
                    Posicion = ubicacion
                };
                nodos.Add(nuevoNodo);
                panelDibujo.Invalidate();
            }
        }

        private bool ExisteNodoEnPosicion(Point ubicacion)
        {
            return nodos.Any(n => 
                Math.Sqrt(Math.Pow(n.Posicion.X - ubicacion.X, 2) + 
                Math.Pow(n.Posicion.Y - ubicacion.Y, 2)) < RadioNodo * 2);
        }

        private void ProcesarClickArista(Point ubicacion)
        {
            var nodoClickeado = ObtenerNodoEnPosicion(ubicacion);
            if (nodoClickeado != null)
            {
                if (nodoTemp == null)
                {
                    nodoTemp = nodoClickeado;
                }
                else if (nodoTemp != nodoClickeado)
                {
                    if (!ExisteArista(nodoTemp, nodoClickeado))
                    {
                        var peso = ObtenerPesoArista();
                        if (peso.HasValue)
                        {
                            aristas.Add(new Arista
                            {
                                Origen = nodoTemp,
                                Destino = nodoClickeado,
                                Peso = peso.Value
                            });
                            panelDibujo.Invalidate();
                        }
                    }
                    nodoTemp = null;
                }
            }
        }

        private double? ObtenerPesoArista()
        {
            using (var form = new Form())
            {
                form.Text = "Peso de la Arista";
                form.Size = new Size(300, 150);
                form.StartPosition = FormStartPosition.CenterParent;

                var txtPeso = new TextBox
                {
                    Location = new Point(10, 20),
                    Width = 260
                };

                var btnAceptar = new Button
                {
                    Text = "Aceptar",
                    DialogResult = DialogResult.OK,
                    Location = new Point(10, 50)
                };

                form.Controls.AddRange(new Control[] { txtPeso, btnAceptar });

                if (form.ShowDialog() == DialogResult.OK && 
                    double.TryParse(txtPeso.Text, out double peso))
                {
                    return peso;
                }
            }
            return null;
        }

        private bool ExisteArista(Nodo origen, Nodo destino)
        {
            return aristas.Any(a => 
                (a.Origen == origen && a.Destino == destino) || 
                (a.Origen == destino && a.Destino == origen));
        }

        private void ProcesarSeleccionNodo(Point ubicacion)
        {
            var nodoClickeado = ObtenerNodoEnPosicion(ubicacion);
            if (nodoClickeado != null)
            {
                if (seleccionandoInicio)
                {
                    nodoInicial = nodoClickeado;
                    seleccionandoInicio = false;
                }
                else if (seleccionandoFin)
                {
                    nodoFinal = nodoClickeado;
                    seleccionandoFin = false;
                }
                panelDibujo.Invalidate();
            }
        }

        private Nodo ObtenerNodoEnPosicion(Point ubicacion)
        {
            return nodos.FirstOrDefault(n =>
                Math.Sqrt(Math.Pow(n.Posicion.X - ubicacion.X, 2) +
                Math.Pow(n.Posicion.Y - ubicacion.Y, 2)) < RadioNodo);
        }

        private void PanelDibujo_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            DibujarAristas(e.Graphics);
            DibujarNodos(e.Graphics);
        }


        private void DibujarAristas(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            //....Pintar primer nodo y último nodo para saber donde va la arista, una vez que se selecciona
            if (nodoTemp != null)
            {
                using (var pen = new Pen(Color.FromArgb(180, 0, 0, 0), 2))
                {
                    g.DrawLine(pen, nodoTemp.Posicion, mousePosicion);
                }
            }

            //....Dibujar nodo más grande seleccionado con un color diferente mientras se selecciona
            if (nodoTemp != null)
            {
                using (var brush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    g.FillEllipse(brush,
                        nodoTemp.Posicion.X - RadioNodo - 2,
                        nodoTemp.Posicion.Y - RadioNodo - 2,
                        RadioNodo * 2 + 4,
                        RadioNodo * 2 + 4);
                }
            }

            //....Al cliquear que muestre otro ícono de mouse
            if (nodoTemp != null)
            {
                Cursor = Cursors.Cross;
            }
            else
            {
                Cursor = Cursors.Default;
            }

            foreach (var arista in aristas)
            {
                using (var pen = new Pen(Color.FromArgb(180, 0, 0, 0), 2))
                {
                    //....dibuja línea principal
                    g.DrawLine(pen, arista.Origen.Posicion, arista.Destino.Posicion);

                    //....dibuja flecha direccional
                    DibujarFlecha(g, arista.Origen.Posicion, arista.Destino.Posicion);
                    //....dibuja flecha punto opuesto
                    DibujarFlecha(g, arista.Destino.Posicion, arista.Origen.Posicion);

                    var puntoMedio = new PointF(
                        (arista.Origen.Posicion.X + arista.Destino.Posicion.X) / 2,
                        (arista.Origen.Posicion.Y + arista.Destino.Posicion.Y) / 2
                    );

                    var pesoTexto = arista.Peso.ToString("F1");
                    var font = new Font("Arial", 9);
                    var size = g.MeasureString(pesoTexto, font);

                    using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                    {
                        g.FillRectangle(brush,
                            puntoMedio.X - size.Width/2 - 2,
                            puntoMedio.Y - size.Height/2 - 2,
                            size.Width + 4,
                            size.Height + 4);
                    }

                    g.DrawString(pesoTexto,
                        font,
                        Brushes.Black,
                        puntoMedio.X - size.Width/2,
                        puntoMedio.Y - size.Height/2);
                }
            }
        }

        private void DibujarFlecha(Graphics g, Point inicio, Point fin)
        {
            var dx = fin.X - inicio.X;
            var dy = fin.Y - inicio.Y;
            var angulo = Math.Atan2(dy, dx);
            var longitud = Math.Sqrt(dx * dx + dy * dy);
    
            var puntoFinal = new Point(
                (int)(inicio.X + (longitud - RadioNodo) * Math.Cos(angulo)),
                (int)(inicio.Y + (longitud - RadioNodo) * Math.Sin(angulo))
            );

            var tamanoFlecha = 10;
            var anguloFlecha = Math.PI / 6;

            var punto1 = new Point(
                (int)(puntoFinal.X - tamanoFlecha * Math.Cos(angulo - anguloFlecha)),
                (int)(puntoFinal.Y - tamanoFlecha * Math.Sin(angulo - anguloFlecha))
            );

            var punto2 = new Point(
                (int)(puntoFinal.X - tamanoFlecha * Math.Cos(angulo + anguloFlecha)),
                (int)(puntoFinal.Y - tamanoFlecha * Math.Sin(angulo + anguloFlecha))
            );

            using (var brush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                g.FillPolygon(brush, new Point[] { puntoFinal, punto1, punto2 });
            }
        }

        private void DibujarNodos(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var nodo in nodos)
            {
                var color = ObtenerColorNodo(nodo);
                
                //....dibuja sombra, más encachado
                using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    g.FillEllipse(shadowBrush,
                        nodo.Posicion.X - RadioNodo + 3,
                        nodo.Posicion.Y - RadioNodo + 3,
                        RadioNodo * 2,
                        RadioNodo * 2);
                }

                //....dibuja el nodo
                using (var brush = new SolidBrush(color))
                using (var pen = new Pen(Color.FromArgb(200, 255, 255, 255), 2))
                {
                    g.FillEllipse(brush,
                        nodo.Posicion.X - RadioNodo,
                        nodo.Posicion.Y - RadioNodo,
                        RadioNodo * 2,
                        RadioNodo * 2);
                    
                    g.DrawEllipse(pen,
                        nodo.Posicion.X - RadioNodo,
                        nodo.Posicion.Y - RadioNodo,
                        RadioNodo * 2,
                        RadioNodo * 2);
                }

                using (var font = new Font("Arial", 10, FontStyle.Bold))
                {
                    var texto = nodo.Id.ToString();
                    var size = g.MeasureString(texto, font);
                    g.DrawString(texto,
                        font,
                        Brushes.White,
                        nodo.Posicion.X - size.Width/2,
                        nodo.Posicion.Y - size.Height/2);
                }

                //....Muestra hint si el mouse está cerca sin parpadeos
                if (mousePosicion != Point.Empty)
                {
                    if (Math.Sqrt(Math.Pow(nodo.Posicion.X - mousePosicion.X, 2) +
                        Math.Pow(nodo.Posicion.Y - mousePosicion.Y, 2)) < RadioNodo * 1.5)
                    {
                        using (var font = new Font("Arial", 8))
                        {
                            var texto = $"Nodo {nodo.Id}";
                            var size = g.MeasureString(texto, font);
                            g.FillRectangle(Brushes.White,
                                nodo.Posicion.X - size.Width/2 - 2,
                                nodo.Posicion.Y - RadioNodo - size.Height - 2,
                                size.Width + 4,
                                size.Height + 4);
                            g.DrawString(texto,
                                font,
                                Brushes.Black,
                                nodo.Posicion.X - size.Width/2,
                                nodo.Posicion.Y - RadioNodo - size.Height);
                        }
                    }
                }
            }

            //....Agrega mi firma
            using (var font = new Font("Arial", 8, FontStyle.Italic))
            {
                g.DrawString("Hecho por FECORO @ Rengo",
                    font,
                    Brushes.Gray,
                    panelDibujo.Width - 162,
                    panelDibujo.Height - 20);
            }
        }


        private Color ObtenerColorNodo(Nodo nodo)
        {
            if (nodo == nodoInicial) return Color.Green;
            if (nodo == nodoFinal) return Color.Red;
            return Color.Blue;
        }

        private void EncontrarYMostrarCamino()
        {
            if (nodoInicial == null || nodoFinal == null)
            {
                MessageBox.Show("Por favor seleccione nodos de inicio y fin.");
                return;
            }

            var (distancias, previos) = AlgoritmoDijkstra(nodoInicial);
            
            if (distancias[nodoFinal] == double.PositiveInfinity)
            {
                MessageBox.Show("No existe un camino entre los nodos seleccionados.");
                return;
            }

            var camino = ReconstruirCamino(previos, nodoFinal);
            MostrarResultado(camino, distancias[nodoFinal]);
        }

        private (Dictionary<Nodo, double> distancias, Dictionary<Nodo, Nodo> previos) 
            AlgoritmoDijkstra(Nodo inicio)
        {
            var distancias = new Dictionary<Nodo, double>();
            var previos = new Dictionary<Nodo, Nodo>();
            var noVisitados = new HashSet<Nodo>();

            foreach (var nodo in nodos)
            {
                distancias[nodo] = double.PositiveInfinity;
                noVisitados.Add(nodo);
            }

            distancias[inicio] = 0;

            while (noVisitados.Count > 0)
            {
                var actual = noVisitados.OrderBy(n => distancias[n]).First();
                noVisitados.Remove(actual);

                foreach (var arista in aristas.Where(a => a.Origen == actual || a.Destino == actual))
                {
                    var vecino = arista.Origen == actual ? arista.Destino : arista.Origen;
                    var distancia = distancias[actual] + arista.Peso;

                    if (distancia < distancias[vecino])
                    {
                        distancias[vecino] = distancia;
                        previos[vecino] = actual;
                    }
                }
            }

            return (distancias, previos);
        }

        private List<Nodo> ReconstruirCamino(Dictionary<Nodo, Nodo> previos, Nodo destino)
        {
            var camino = new List<Nodo>();
            var actual = destino;

            while (actual != null)
            {
                camino.Add(actual);
                previos.TryGetValue(actual, out actual);
            }

            camino.Reverse();
            return camino;
        }

        private void MostrarResultado(List<Nodo> camino, double distanciaTotal)
        {
            var mensaje = $"Distancia total: {distanciaTotal}\n\nCamino: ";
            mensaje += string.Join(" → ", camino.Select(n => n.Id));
            MessageBox.Show(mensaje, "Resultado");
        }

        private void LimpiarTodo()
        {
            nodos.Clear();
            aristas.Clear();
            nodoInicial = null;
            nodoFinal = null;
            nodoTemp = null;
            DesactivarTodosModos();
            panelDibujo.Invalidate();
        }

        //....Clases auxiliares
        private class Nodo
        {
            public int Id { get; set; }
            public Point Posicion { get; set; }
        }

        private class Arista
        {
            public Nodo Origen { get; set; }
            public Nodo Destino { get; set; }
            public double Peso { get; set; }
        }

        //.....Las variables de la interfaz
        private Panel panelDibujo;
        private Panel panelControl;
        private Button btnAgregarNodo;
        private Button btnAgregarArista;
        private Button btnSeleccionarInicio;
        private Button btnSeleccionarFin;
        private Button btnEncontrarCamino;
        private Button btnLimpiar;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormPrincipal());
        }
    }
}