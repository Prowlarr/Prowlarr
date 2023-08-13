import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditApplicationModalConnector from './EditApplicationModalConnector';
import styles from './Application.css';

class Application extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditApplicationModalOpen: false,
      isDeleteApplicationModalOpen: false
    };
  }

  //
  // Listeners

  onEditApplicationPress = () => {
    this.setState({ isEditApplicationModalOpen: true });
  };

  onEditApplicationModalClose = () => {
    this.setState({ isEditApplicationModalOpen: false });
  };

  onDeleteApplicationPress = () => {
    this.setState({
      isEditApplicationModalOpen: false,
      isDeleteApplicationModalOpen: true
    });
  };

  onDeleteApplicationModalClose= () => {
    this.setState({ isDeleteApplicationModalOpen: false });
  };

  onConfirmDeleteApplication = () => {
    this.props.onConfirmDeleteApplication(this.props.id);
  };

  //
  // Render

  render() {
    const {
      id,
      name,
      syncLevel,
      fields,
      tags,
      tagList
    } = this.props;

    const applicationUrl = fields.find((field) => field.name === 'baseUrl')?.value;

    return (
      <Card
        className={styles.application}
        overlayContent={true}
        onPress={this.onEditApplicationPress}
      >
        <div className={styles.nameContainer}>
          <div className={styles.name}>
            {name}
          </div>

          {
            applicationUrl ?
              <IconButton
                className={styles.externalLink}
                name={icons.EXTERNAL_LINK}
                title={translate('GoToApplication')}
                to={`${applicationUrl}`}
              /> : null
          }
        </div>

        {
          syncLevel === 'addOnly' &&
            <Label kind={kinds.WARNING}>
              {translate('AddRemoveOnly')}
            </Label>
        }

        {
          syncLevel === 'fullSync' &&
            <Label kind={kinds.SUCCESS}>
              {translate('FullSync')}
            </Label>
        }

        {
          syncLevel === 'disabled' &&
            <Label
              kind={kinds.DISABLED}
              outline={true}
            >
              {translate('Disabled')}
            </Label>
        }

        <TagList
          tags={tags}
          tagList={tagList}
        />

        <EditApplicationModalConnector
          id={id}
          isOpen={this.state.isEditApplicationModalOpen}
          onModalClose={this.onEditApplicationModalClose}
          onDeleteApplicationPress={this.onDeleteApplicationPress}
        />

        <ConfirmModal
          isOpen={this.state.isDeleteApplicationModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteApplication')}
          message={translate('DeleteApplicationMessageText', { name })}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDeleteApplication}
          onCancel={this.onDeleteApplicationModalClose}
        />
      </Card>
    );
  }
}

Application.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  syncLevel: PropTypes.string.isRequired,
  fields: PropTypes.arrayOf(PropTypes.object).isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  tagList: PropTypes.arrayOf(PropTypes.object).isRequired,
  onConfirmDeleteApplication: PropTypes.func
};

export default Application;
