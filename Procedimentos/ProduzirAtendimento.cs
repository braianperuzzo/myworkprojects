using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Avant.Analysing;
using Avant.Core.Evaluator;
using Avant.Utils;
using Avant.Data.Adapter;
using Avant.Core;
using Avant.Core.Xml;
using Avant.Common;
using Avant.Communicator;
using Avant.DataTypes;
using Avant.Managers;
using Avant.BO;
using Avant.BO.Bases;
using Avant.BO.Admin;
using Avant.Presentation;
using Avant.UI.WF;

namespace Avant.Customization.Internals
{
    public static partial class ProcedimentosCustomizados
    {
        public static void ProduzirAtendimento(object caller, params object[] args)
        {
            try
            {
                
                if (Arrays.IsNullOrEmpty(args))
                    return;
                
                object[] parameters = args[0] as object[];
                
                if (Arrays.IsNullOrEmpty(parameters))
                    return;
                
                List<object> itens = new List<object>();
                
                foreach (object item in parameters)
                {
                    if (item is IEnumerable && !(item is string))
                        itens.AddRange(item as IEnumerable);
                    else
                        itens.Add(item);
                } 
                
                //Configuração padrão   
                if (Arrays.IsNullOrEmpty(args))
                    return;
                
                List<object> objetos = new List<object>();
                
                foreach (object item in args)
                {
                    if (item is IEnumerable && !(item is string))
                        objetos.AddRange(item as IEnumerable);
                    else
                        objetos.Add(item);
                } 
                
                Avant.BO.Servico.Atendimento atendimento = objetos[0] as Avant.BO.Servico.Atendimento;
                
                //Bloqueia Produzir com cliente que não esteja Ativo
                if(atendimento.Pessoa.Situacao.ToString() != "Ativo")
                    throw new OperacaoNaoSuportada("ATENÇÃO! NÃO FOI POSSÍVEL PRODUZIR!\r\nO cliente não está na situação ATIVO. Solicite a ativação antes de continuar."); 
                
                //Bloqueia produzir com a Tranprotadora Rodonaves e HB Tranpsporte
                if(Strings.ContainsAny(atendimento.Transportador.Nome, "RODONAVES", "HB TRANSPORTES"))
                    throw new OperacaoNaoSuportada("ATENÇÃO! NÃO FOI POSSÍVEL PRODUZIR!\r\nA transportadora {0} não é aceita para esse documento pois possui restrição para retornos de conserto.", atendimento.Transportador.Nome); 
                
                //Define parametros para os erros que impedem produzir
                bool bSemPreenchimento = true;
                StringBuilder errosempreenchimento = new StringBuilder();
                
                //Confere se há dados nas colunas DS_TRANSPORTADORA, DS_FALHA, DS_CAUSA, DS_LOCALFALHA, DS_CAUSADETALHADA e DS_CRITICIDADE da tabela _USR_AT_DOCUMENTO
                string sInformacoes = string.Empty;
                sInformacoes = string.Format(@"SELECT DS_TRANSPORTADORA, DS_FALHA, DS_CAUSA, DS_LOCALFALHA, DS_CAUSADETALHADA, DS_GARANTIA, DS_CRITICIDADE
                FROM _USR_AT_DOCUMENTO
                WHERE CD_EMPRESA = '{0}'
                AND CD_DOCUMENTO = {1}
                AND NR_COMPL = '{2}'",
                atendimento.CodigoEmpresa, atendimento.Numero.ToString(), atendimento.Complemento);
                Avant.Data.DataResult dataResultInformacoes = new Avant.Data.DataResult();
                Avant.Data.Adapter.DataAccess.Current.ExecuteResult(sInformacoes, ref dataResultInformacoes);
                dataResultInformacoes.Read();
                string sTemTransportador = dataResultInformacoes.GetString("DS_TRANSPORTADORA");
                string sTemFalha = dataResultInformacoes.GetString("DS_FALHA");
                string sTemCausa = dataResultInformacoes.GetString("DS_CAUSA");
                string sTemLocalFalha = dataResultInformacoes.GetString("DS_LOCALFALHA");
                string sTemCausaDetalhada = dataResultInformacoes.GetString("DS_CAUSADETALHADA");
                string sTemGarantia = dataResultInformacoes.GetString("DS_GARANTIA");
                string sTemCriticidade = dataResultInformacoes.GetString("DS_CRITICIDADE");
                
                //Se não houver dados nas colunas consultadas acima, grava um erro específico para o responsável saber o que deve ser preenchido
                if(String.IsNullOrEmpty(sTemTransportador))
                {
                    bSemPreenchimento = false;
                    errosempreenchimento.AppendLine("É necessário preencher as informações do cliente pelo botão Informações Cliente.");
                }
                
                if(String.IsNullOrEmpty(sTemFalha))
                {
                    bSemPreenchimento = false;
                    errosempreenchimento.AppendLine("É necessário preencher a Falha.");
                }
                
                if(String.IsNullOrEmpty(sTemCausa))
                {
                    bSemPreenchimento = false;
                    errosempreenchimento.AppendLine("É necessário preencher a Causa.");
                }
                
                if(String.IsNullOrEmpty(sTemLocalFalha))
                {
                    bSemPreenchimento = false;
                    errosempreenchimento.AppendLine("É necessário preencher o Local da Falha.");
                }
                
                if(String.IsNullOrEmpty(sTemCausaDetalhada))
                {
                    bSemPreenchimento = false;
                    errosempreenchimento.AppendLine("É necessário preencher a Causa Detalhada.");
                }
                
                if(String.IsNullOrEmpty(sTemGarantia))
                {
                    bSemPreenchimento = false;
                    errosempreenchimento.AppendLine("É necessário preencher a Garantia.");
                }
                
                if(String.IsNullOrEmpty(sTemCriticidade))
                {
                    bSemPreenchimento = false;
                    errosempreenchimento.AppendLine("É necessário preencher a Criticidade");
                }
                
                //Exibe o erro, com as informações do que deve ser preenchido, conforme os campos acima.
                if(String.IsNullOrEmpty(sTemTransportador) || String.IsNullOrEmpty(sTemCausaDetalhada) || String.IsNullOrEmpty(sTemFalha) || String.IsNullOrEmpty(sTemCausa) || String.IsNullOrEmpty(sTemLocalFalha) || String.IsNullOrEmpty(sTemGarantia) || String.IsNullOrEmpty(sTemCriticidade))
                    throw new OperacaoNaoSuportada("ATENÇÃO! NÃO FOI POSSÍVEL PRODUZIR!\r\n{0}", errosempreenchimento.ToString());
                
                //Se os campos estiverem preenchidos, produz
                if(!String.IsNullOrEmpty(sTemTransportador) || !String.IsNullOrEmpty(sTemCausaDetalhada) || !String.IsNullOrEmpty(sTemFalha) || !String.IsNullOrEmpty(sTemCausa) || !String.IsNullOrEmpty(sTemLocalFalha) || !String.IsNullOrEmpty(sTemGarantia) || !String.IsNullOrEmpty(sTemCriticidade))
                {
                    bool ret = true;
                    
                    Avant.Managers.PresentationManager.Current.ShowProgressMessage("Gerando Pedido de Produção(s). Aguarde...");
                    
                    Avant.BO.Producao.Pedido novoDocumento = null;
                    
                    string sNumeroPedidoProducao = string.Empty;
                    string sComplementoPedidoProducao = string.Empty;
                    
                    foreach (Avant.BO.Servico.Atendimento item in itens) 
                    {
                       
                        if (item.DocumentoDestinoTipo == "Avant.BO.Producao.Pedido")
                        {
                            sNumeroPedidoProducao = item.DocumentoDestinoNumero.ToString();
                            sComplementoPedidoProducao = item.DocumentoDestinoComplemento;
                        }
                        
                        bool trans = DataAccess.ThreadCurrent.BeginTransaction();
                        
                        if (item.DocumentoDestinoTipo == "Avant.BO.Producao.Pedido" && novoDocumento == null)
                        {
                            Avant.Managers.PresentationManager.Current.CloseProgressMessage();
                            
                            Avant.Managers.PresentationManager.Current.ShowCritical("Este pedido já foi faturado!", "Faturar Pedido");
                            ret = false;
                            goto fim;
                        }                    
                        
                        string nomeTipo = "Avant.BO.Producao.Pedido";
                        
                        Type tipo = TypeUtils.SearchTypeFromName(nomeTipo);
                        
                        if (tipo == null)
                        {
                            Avant.Managers.PresentationManager.Current.CloseProgressMessage();
                            Avant.Managers.PresentationManager.Current.ShowCritical(string.Format("O tipo de documento definido '{0}' não foi encontrado!", nomeTipo),  "Faturar Atendimento");
                            ret = false;
                            goto fim;
                        }
                        
                        if (!tipo.IsSubclassOf(typeof(Avant.BO.Admin.Bases.DoctoBase)))
                        {
                            Avant.Managers.PresentationManager.Current.CloseProgressMessage();
                            Avant.Managers.PresentationManager.Current.ShowCritical(string.Format("O tipo de documento definido '{0}' não é um descendente do documento base!", nomeTipo), "Faturar Atendimento");
                            ret = false;
                            goto fim;
                        }
                        
                        if (novoDocumento == null)
                        {
                            novoDocumento = EntidadeConversor.Converter(item, tipo) as Avant.BO.Producao.Pedido;
                            
                            foreach(Avant.BO.Producao.PedidoItem itemPedido in novoDocumento.Itens)
                            {
                                if (itemPedido.Produto.Servico == enumIndicador.Sim)
                                    novoDocumento.Itens.Remove(itemPedido);
                            }
                            
                            novoDocumento.Complemento = "0";
                            
                            if (PermissionManager.CurrentCompany.Id == "001")
                                novoDocumento.Complemento = "PP";
                            
                            if (PermissionManager.CurrentCompany.Id == "003")
                                novoDocumento.Complemento = "PP-SP";
                            
                            novoDocumento.DocumentoOrigemNumero = item.Numero;
                            novoDocumento.DocumentoOrigemComplemento = item.Complemento;
                            novoDocumento.DocumentoOrigemTipo  = "Avant.BO.Servico.Atendimento";
                            
                            novoDocumento.Observacao = string.Empty;
                            novoDocumento.DataPrevisao = item.DataPrevisao;
                            novoDocumento.Atributo3 = item.Atributo7;
                            novoDocumento.Atributo5 = string.Empty;
                            
                            
                            
                        }
                        else
                            break;
                        
                        ret = novoDocumento.Grava();
                        
                        
                        
                        if (!ret)
                        {
                            Avant.Managers.PresentationManager.Current.CloseProgressMessage();
                            
                            Avant.Managers.PresentationManager.Current.ShowCritical(novoDocumento.LastException.ToString(), "Gravar Pedido");
                            
                            ret = false;
                            goto fim;
                        }
                        else
                        {
                            // Avança Etapa para AT Em Produção
                            Avant.BO.Servico.AtendimentoEtapa etapaAtEmProducao = new Avant.BO.Servico.AtendimentoEtapa(6);
                            
                            item.DefineEtapa(etapaAtEmProducao);
                            
                            // Adiciona histórico
                            
                            Avant.BO.Servico.AtendimentoHistorico historico =  new Avant.BO.Servico.AtendimentoHistorico();
                            
                            historico.Origem = enumOrigemHistorico.Sistema;
                            historico.Descricao = string.Format("GERADO PEDIDO DE PRODUÇAO {0}/{1}", novoDocumento.Numero, novoDocumento.Complemento);
                            
                            item.Historicos.Add(historico);
                            
                        item.Observacao = string.Concat(item.Observacao,  string.Format("\r\nGERADO PEDIDO DE PRODUÇAO: {0}/{1}", novoDocumento.Numero, novoDocumento.Complemento));
                        
                        
                        novoDocumento.AlteraValorPropriedade("Atributo3", item.Atributo7);
                        
                        item.AlteraValorPropriedade("DocumentoDestinoNumero", novoDocumento.Numero);
                        item.AlteraValorPropriedade("DocumentoDestinoComplemento", novoDocumento.Complemento);
                        item.AlteraValorPropriedade("DocumentoDestinoTipo", novoDocumento.GetType().FullName);                        
                        
                        if (ret)
                            ret = item.GravaEntidade();
                    }
                    
                fim:
                    
                    if (trans)
                    {
                        if (ret)
                        {
                            DataAccess.ThreadCurrent.CommitTransaction();
                            
                            
                            if (novoDocumento != null)
                                Managers.PresentationManager.Current.ShowEntity(novoDocumento, true);
                            
                            
                        }
                        else
                        {
                            Avant.Managers.PresentationManager.Current.CloseProgressMessage();
                            
                            DataAccess.ThreadCurrent.RollbackTransaction();
                            
                            break;
                        }
                    }
                }
                
                Avant.Managers.PresentationManager.Current.CloseProgressMessage();
            }   
        }
        catch (Exception ex)
        {
            Avant.Managers.PresentationManager.Current.ShowMessage("Falha ao executar procedimento customizado '{0}'!", 
            "Execução de CodeOn", ex, "Fatura");
        }
    }
}
}
