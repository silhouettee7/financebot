using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Handlers;

public class DialogHandler(PDbContext dbContext,
    IEnumerable<IDialogDefinition> dialogs,
    ITelegramBotClient botClient): IRequestHandler<StartDialogRequest>, IRequestHandler<ProcessDialogRequest>
{
    public async Task Handle(StartDialogRequest request, CancellationToken cancellationToken)
    {
        var dialogDefinition = dialogs.FirstOrDefault(dlg => dlg.DialogName == request.DialogName);
        if (dialogDefinition == null)
            return;
        var dialogContext = await dbContext.Dialogs.FirstOrDefaultAsync(dlg => dlg.UserId == request.UserId) ?? new DialogContext();
        dialogContext.DialogStorage = new Dictionary<string, object>();
        dialogContext.DialogName = request.DialogName;
        dialogContext.UserId = request.UserId;
        dialogContext.CurrentStep = 0;
        dialogContext.PrevStep = -1;
        var step = dialogDefinition.Steps[dialogContext.CurrentStep];
        if (!await TryPrompt(request.UserId, step, request.Update, dialogContext, cancellationToken))
            return;

        if (dialogContext.Id == 0)
        {
            await dbContext.Dialogs.AddAsync(dialogContext);
        }
        else
        {
            dbContext.Dialogs.Update(dialogContext);
        }
        
        await dbContext.SaveChangesAsync();
    }

    public async Task Handle(ProcessDialogRequest request, CancellationToken cancellationToken)
    {
        var update = request.Update;
        var dialogContext = request.DialogContext;
        var dialogDefinition = dialogs.FirstOrDefault(dlg => dlg.DialogName == dialogContext.DialogName);
        if (dialogDefinition == null)
            return;
        if (update.CallbackQuery is { Data: not null } query
            && query.Data.StartsWith("dlg__back"))
        {
            var prevStepIndex = dialogContext.PrevStep;
            if (query.Data.Split('/')[1] != dialogContext.DialogName
                || !dialogDefinition.Steps.TryGetValue(prevStepIndex, out var prevStep))
                return;
            await botClient.AnswerCallbackQuery(query.Id, cancellationToken: cancellationToken);
            dialogContext.DialogStorage?.Remove(dialogDefinition.Steps[dialogContext.CurrentStep].Key);
            dialogContext.CurrentStep = prevStepIndex;
            dialogContext.PrevStep = prevStep.PrevStepId(dialogContext);
            if (!await TryPrompt(query.From.Id, prevStep, update, dialogContext, cancellationToken))
                return;
            dbContext.Dialogs.Update(dialogContext);
            await dbContext.SaveChangesAsync();
            return;
        }

        var handleStep = dialogDefinition.Steps[dialogContext.CurrentStep];
        var handleResult = await handleStep
            .HandleAsync(botClient, update, dialogContext, cancellationToken);
        if (handleResult is { IsSuccess: false, ErrorMessage: not null, ErrorType: ErrorType.Validation })
        {
            await botClient.SendMessage(dialogContext.UserId, 
                handleResult.ErrorMessage,
                parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken);
            if (!await TryPrompt(dialogContext.UserId, handleStep, update, dialogContext, cancellationToken))
                return;
            dbContext.Dialogs.Update(dialogContext);
            await dbContext.SaveChangesAsync();
            return;
        }

        if (!handleResult.IsSuccess)
        {
            return;
        }

        var nextStepId = handleStep
            .NextStepId(dialogContext);
        if (dialogDefinition
            .Steps
            .TryGetValue(nextStepId, out var nextStep))
        {
            var prevStep = dialogContext.PrevStep;
            (dialogContext.PrevStep, dialogContext.CurrentStep) = (dialogContext.CurrentStep, nextStepId);
            if (!await TryPrompt(dialogContext.UserId, nextStep, update, dialogContext, cancellationToken))
            {
                (dialogContext.PrevStep, dialogContext.CurrentStep) = (prevStep, dialogContext.PrevStep);
                return;
            }
            dbContext.Dialogs.Update(dialogContext);
            await dbContext.SaveChangesAsync();
            return;
        }

        if (nextStepId == -1)
        {
            await dialogDefinition.OnCompletedAsync(dialogContext.UserId, dialogContext, update, cancellationToken);
            await dbContext.SaveChangesAsync();
        }
    }
    
    private async Task<bool> TryPrompt(long userId, IStep step, Update update,
        DialogContext dialogContext, CancellationToken cancellationToken)
    {
        var promptResult = await step
            .PromptAsync(botClient, userId, dialogContext, cancellationToken);
        if (promptResult.IsSuccess) return true;
        if (step.OnPromptFailed != null)
            await step.OnPromptFailed(promptResult, userId, update, dialogContext);
        return false;
    }
}