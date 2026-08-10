from dataclasses import dataclass
from datetime import datetime, date

# Sempre deixe o dict gerado com nomes camelCase!

@dataclass
class User:
    name: str
    birth_date: date

    def to_dict(self) -> dict:
        birth_str = self.birth_date.strftime('%d/%m/%Y') if isinstance(self.birth_date, (date, datetime)) else str(self.birth_date)
        return {
            'name': self.name,
            'birthDate': birth_str
        }

    @classmethod
    def from_dict(cls, data: dict) -> 'User':
        birth_date_val = data.get('birthDate')
        if isinstance(birth_date_val, str):
            parsed_date = datetime.strptime(birth_date_val, '%d/%m/%Y').date()
        elif isinstance(birth_date_val, datetime):
            parsed_date = birth_date_val.date()
        elif isinstance(birth_date_val, date):
            parsed_date = birth_date_val
        else:
            parsed_date = date.today()
            
        return cls(
            name=data.get('name', ''),
            birth_date=parsed_date
        )


@dataclass
class UserAstrology(User):
    week_day: str = ''
    sign: str = ''

    def to_dict(self) -> dict:
        data = super().to_dict()
        data.update({
            'weekDay': self.week_day,
            'sign': self.sign
        })
        return data

    @classmethod
    def from_dict(cls, data: dict) -> 'UserAstrology':
        user = super().from_dict(data)
        return cls(
            name=user.name,
            birth_date=user.birth_date,
            week_day=data.get('weekDay', ''),
            sign=data.get('sign', '')
        )


@dataclass
class Talk:
    author: str
    text: str
    reply: bool = False

    def to_dict(self) -> dict:
        return {
            'author': self.author,
            'text': self.text,
            'reply': self.reply
        }

    @classmethod
    def from_dict(cls, data: dict) -> 'Talk':
        return cls(
            author=data.get('author', ''),
            text=data.get('text', ''),
            reply=data.get('reply', False)
        )
