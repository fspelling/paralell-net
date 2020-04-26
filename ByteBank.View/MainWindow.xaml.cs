using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            _cts = new CancellationTokenSource();
            BtnProcessar.IsEnabled = false;
            BtnCancelar.IsEnabled = true;

            var contas = r_Repositorio.GetContaClientes();
            PgsProgresso.Maximum = contas.Count();

            LimparView();

            var inicio = DateTime.Now;

            try
            {
                var resultado = await ConsolidarContas(contas, new Progress<string>(str => PgsProgresso.Value++), _cts.Token);
                var fim = DateTime.Now;
                AtualizarView(resultado, fim - inicio);
            }
            catch (OperationCanceledException)
            {
                TxtProgresso.Text = "Cancelamento ralizado com sucesso";
            }
            finally
            {
                BtnProcessar.IsEnabled = true;
                BtnCancelar.IsEnabled = false;
            }
        }

        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas, IProgress<string> progress, CancellationToken ct)
        {
            var tarefas = contas.Select(conta => Task.Factory.StartNew(() =>
            {
                ct.ThrowIfCancellationRequested();
                var resultadoConsolidacao = r_Servico.ConsolidarMovimentacao(conta, ct);
                ct.ThrowIfCancellationRequested();

                progress.Report(resultadoConsolidacao);
                return resultadoConsolidacao;
            }, ct));

            return await Task.WhenAll(tarefas);
        }

        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = null;
            PgsProgresso.Value = 0;
        }

        private void AtualizarView(IEnumerable<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelar.IsEnabled = false;
            _cts.Cancel();
        }
    }
}
