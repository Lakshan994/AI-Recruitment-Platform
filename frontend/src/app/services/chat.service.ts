import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface ChatMessage {
  id: string;
  sender: 'user' | 'bot';
  text: string;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = 'http://localhost:5076/api/chat';
  
  // State for the chat widget visibility
  isChatOpen = signal<boolean>(false);
  
  // Conversation history
  messages = signal<ChatMessage[]>([]);

  constructor(private http: HttpClient) {
    // Add initial greeting message
    this.messages.set([
      {
        id: crypto.randomUUID(),
        sender: 'bot',
        text: 'Hi there! I am TalentAI Assistant. How can I help you use our platform today?',
        timestamp: new Date()
      }
    ]);
  }

  toggleChat(): void {
    this.isChatOpen.set(!this.isChatOpen());
  }

  sendMessage(text: string): Observable<void> {
    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      sender: 'user',
      text,
      timestamp: new Date()
    };

    // Optimistically add user message
    this.messages.update(msgs => [...msgs, userMessage]);

    return this.http.post<{ reply: string }>(this.apiUrl, { message: text }).pipe(
      map(response => {
        const botMessage: ChatMessage = {
          id: crypto.randomUUID(),
          sender: 'bot',
          text: response.reply,
          timestamp: new Date()
        };
        // Add bot message
        this.messages.update(msgs => [...msgs, botMessage]);
      })
    );
  }
}
